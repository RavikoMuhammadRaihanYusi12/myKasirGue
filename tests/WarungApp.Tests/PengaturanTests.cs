using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;
using WarungApp.Data;
using Xunit;

namespace WarungApp.Tests;

public class PengaturanTests : IDisposable
{
    private readonly string _dir;
    private readonly SqliteConnection _conn;
    private readonly ServiceProvider _sp;

    public PengaturanTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "warungtest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        DbPath.OverrideFolder = _dir;

        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_conn));
        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite(_conn));
        services.AddSingleton<CurrentUserContext>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserContext>());
        services.AddSingleton<IAuthService, WarungApp.Core.Services.AuthService>();
        services.AddScoped<IBarangService, WarungApp.Core.Services.BarangService>();
        services.AddScoped<IStokService, WarungApp.Core.Services.StokService>();
        services.AddScoped<IPenjualanService, WarungApp.Core.Services.PenjualanService>();
        services.AddScoped<ISettingsService, WarungApp.Core.Services.SettingsService>();
        services.AddScoped<IAuditService, WarungApp.Core.Services.AuditService>();
        services.AddScoped<IUserService, WarungApp.Core.Services.UserService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        _sp = services.BuildServiceProvider();
        using var scope = _sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    private async Task LoginPemilikAsync()
    {
        var auth = _sp.GetRequiredService<IAuthService>();
        await auth.SetupOwnerAsync("pemilik", "Budi", "rahasia123");
        await auth.LoginAsync("pemilik", "rahasia123", WarungApp.Core.Enums.Role.Pemilik);
    }

    [Fact]
    public async Task Pengaturan_Default_Dan_Simpan()
    {
        await LoginPemilikAsync();
        var svc = _sp.GetRequiredService<ISettingsService>();

        var awal = await svc.AmbilAsync();
        Assert.Equal("Warung Saya", awal.NamaToko);
        Assert.False(awal.BolehStokMinus);
        Assert.Equal(14, awal.RetensiBackup);

        var simpan = await svc.SimpanAsync(new PengaturanDto("Warung Bu Budi", "Jl. Mawar", "0812", true, "", 7));
        Assert.True(simpan.Success, simpan.Error);

        var akhir = await svc.AmbilAsync();
        Assert.Equal("Warung Budi" == "x" ? "" : "Warung Bu Budi", akhir.NamaToko);
        Assert.True(akhir.BolehStokMinus);
        Assert.Equal(7, akhir.RetensiBackup);
    }

    [Fact]
    public async Task BolehStokMinus_Mengizinkan_PenjualanLewatStok()
    {
        await LoginPemilikAsync();
        var barang = _sp.GetRequiredService<IBarangService>();
        var kat = await barang.SimpanKategoriAsync(null, "Umum");
        var b = await barang.SimpanAsync(null, new BarangInput("A1", "Kopi", kat.Value, "pcs", 1000, 2000, 1));

        var jual = _sp.GetRequiredService<IPenjualanService>();
        await Assert.ThrowsAsync<AppException>(() =>
            jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(b.Value!.Id, 2)], 10000)));

        await _sp.GetRequiredService<ISettingsService>()
            .SimpanAsync(new PengaturanDto("W", "", "", true, "", 14));

        var hasil = await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(b.Value!.Id, 2)], 10000));
        Assert.True(hasil.Success, hasil.Error);

        var cari = await barang.CariAsync("kopi", null, false, false);
        Assert.Equal(-2, cari.Value![0].Stok);
    }

    [Fact]
    public async Task Audit_MencatatLoginVoidDanBarang_Dan_KasirTidakBolehBaca()
    {
        await LoginPemilikAsync();
        var barang = _sp.GetRequiredService<IBarangService>();
        var kat = await barang.SimpanKategoriAsync(null, "Umum");
        var b = await barang.SimpanAsync(null, new BarangInput("A2", "Roti", kat.Value, "pcs", 2000, 3000, 1));
        await _sp.GetRequiredService<IStokService>().SimpanStokMasukAsync(
            new StokMasukInput(DateTime.Today, null, null, false, [new StokMasukItemInput(b.Value!.Id, 5, 2000)]));

        var jual = _sp.GetRequiredService<IPenjualanService>();
        var trx = await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(b.Value.Id, 1)], 5000));
        await jual.VoidAsync(trx.Value!.Id, "uji void");

        var audit = _sp.GetRequiredService<IAuditService>();
        var log = await audit.DaftarAsync(null, null);
        Assert.True(log.Success, log.Error);
        var aksi = log.Value!.Select(l => l.Aksi).ToList();
        Assert.Contains("Login", aksi);
        Assert.Contains("Simpan barang", aksi);
        Assert.Contains("Stok masuk", aksi);
        Assert.Contains("Transaksi", aksi);
        Assert.Contains("Void transaksi", aksi);

        var auth = _sp.GetRequiredService<IAuthService>();
        await auth.TambahKasirAsync("kasir", "Siti", "rahasia123");
        _sp.GetRequiredService<ICurrentUser>().Clear();
        await auth.LoginAsync("kasir", "rahasia123", WarungApp.Core.Enums.Role.Kasir);

        await Assert.ThrowsAsync<ForbiddenException>(() => audit.DaftarAsync(null, null));
    }

    [Fact]
    public async Task UserKasir_DibuatDinonaktifkan_TidakBisaLogin()
    {
        await LoginPemilikAsync();
        var users = _sp.GetRequiredService<IUserService>();

        var tambah = await users.TambahKasirAsync("kasir1", "Ani", "rahasia123");
        Assert.True(tambah.Success, tambah.Error);

        var daftar = await users.DaftarAsync();
        Assert.Equal(2, daftar.Value!.Count);

        var nonaktif = await users.SetAktifAsync(tambah.Value, false);
        Assert.True(nonaktif.Success, nonaktif.Error);

        var auth = _sp.GetRequiredService<IAuthService>();
        _sp.GetRequiredService<ICurrentUser>().Clear();
        var login = await auth.LoginAsync("kasir1", "rahasia123");
        Assert.False(login.Success);
    }

    public void Dispose()
    {
        _sp.Dispose();
        _conn.Dispose();
        DbPath.OverrideFolder = null;
        try { Directory.Delete(_dir, true); } catch { }
    }
}

