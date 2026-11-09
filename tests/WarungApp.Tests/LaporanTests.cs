using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;
using WarungApp.Data;
using Xunit;

namespace WarungApp.Tests;

public class LaporanTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ServiceProvider _sp;

    public LaporanTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_conn));
        services.AddSingleton<CurrentUserContext>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserContext>());
        services.AddSingleton<IAuthService, WarungApp.Core.Services.AuthService>();
        services.AddScoped<IBarangService, WarungApp.Core.Services.BarangService>();
        services.AddScoped<IStokService, WarungApp.Core.Services.StokService>();
        services.AddScoped<IPenjualanService, WarungApp.Core.Services.PenjualanService>();
        services.AddScoped<ILaporanService, WarungApp.Core.Services.LaporanService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        _sp = services.BuildServiceProvider();
        using var scope = _sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    private async Task<(int Indomie, int Teh)> SiapkanAsync()
    {
        var auth = _sp.GetRequiredService<IAuthService>();
        await auth.SetupOwnerAsync("pemilik", "Budi", "rahasia123");
        await auth.LoginAsync("pemilik", "rahasia123", WarungApp.Core.Enums.Role.Pemilik);

        var barang = _sp.GetRequiredService<IBarangService>();
        var kat = await barang.SimpanKategoriAsync(null, "Umum");
        var a = await barang.SimpanAsync(null, new BarangInput("A1", "Indomie", kat.Value, "pcs", 3000, 4000, 5));
        var b = await barang.SimpanAsync(null, new BarangInput("A2", "Teh Botol", kat.Value, "pcs", 2000, 3000, 5));

        var stok = _sp.GetRequiredService<IStokService>();
        await stok.SimpanStokMasukAsync(new StokMasukInput(DateTime.Today, null, null, true,
            [new StokMasukItemInput(a.Value!.Id, 20, 3000), new StokMasukItemInput(b.Value!.Id, 10, 2000)]));
        return (a.Value.Id, b.Value.Id);
    }

    [Fact]
    public async Task Ringkasan_OmzetLabaCocok()
    {
        var (indomie, teh) = await SiapkanAsync();
        var jual = _sp.GetRequiredService<IPenjualanService>();
        await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(indomie, 2)], 10000));
        await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(indomie, 1), new PenjualanItemInput(teh, 1)], 10000));

        var laporan = _sp.GetRequiredService<ILaporanService>();
        var r = await laporan.RingkasanAsync(DateTime.Today, DateTime.Today);

        Assert.True(r.Success, r.Error);
        Assert.Equal(8000 + 7000, r.Value!.Omzet);
        Assert.Equal(2000 + 1000 + 1000, r.Value.LabaKotor);
        Assert.Equal(2, r.Value.JumlahTransaksi);
        Assert.Equal((20 - 3) * 3000L + (10 - 1) * 2000L, r.Value.NilaiStok);
    }

    [Fact]
    public async Task Void_TidakDihitungDiLaporan()
    {
        var (indomie, _) = await SiapkanAsync();
        var jual = _sp.GetRequiredService<IPenjualanService>();
        var trx = await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(indomie, 2)], 10000));
        await jual.VoidAsync(trx.Value!.Id, "salah");

        var laporan = _sp.GetRequiredService<ILaporanService>();
        var r = await laporan.RingkasanAsync(DateTime.Today, DateTime.Today);
        Assert.Equal(0, r.Value!.Omzet);
        Assert.Equal(0, r.Value.JumlahTransaksi);

        var dashboard = await laporan.DashboardAsync();
        Assert.Equal(0, dashboard.Value!.OmzetHariIni);
    }

    [Fact]
    public async Task Terlaris_TidakLaku_Dan_StokMenipis()
    {
        var (indomie, _) = await SiapkanAsync();
        var jual = _sp.GetRequiredService<IPenjualanService>();
        await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(indomie, 18)], 100000));

        var laporan = _sp.GetRequiredService<ILaporanService>();
        var terlaris = await laporan.TerlarisAsync(DateTime.Today, DateTime.Today);
        Assert.Equal("Indomie", terlaris.Value![0].Barang);
        Assert.Equal(18, terlaris.Value![0].Qty);

        var tidakLaku = await laporan.TidakLakuAsync(DateTime.Today, DateTime.Today);
        Assert.Single(tidakLaku.Value!);
        Assert.Equal("Teh Botol", tidakLaku.Value![0].Barang);

        // Indomie: 20 - 18 = 2 <= min 5 -> menipis
        var dashboard = await laporan.DashboardAsync();
        Assert.Equal(1, dashboard.Value!.JumlahStokMenipis);
        Assert.Equal("Indomie", dashboard.Value.StokMenipis[0].Barang);
        Assert.Single(dashboard.Value.TerlarisHariIni);
    }

    [Fact]
    public async Task Kasir_TidakBolehLihatLaporan()
    {
        var (indomie, _) = await SiapkanAsync();
        var auth = _sp.GetRequiredService<IAuthService>();
        await auth.TambahKasirAsync("kasir", "Siti", "rahasia123");
        _sp.GetRequiredService<ICurrentUser>().Clear();
        await auth.LoginAsync("kasir", "rahasia123", WarungApp.Core.Enums.Role.Kasir);

        var laporan = _sp.GetRequiredService<ILaporanService>();
        await Assert.ThrowsAsync<ForbiddenException>(() => laporan.RingkasanAsync(DateTime.Today, DateTime.Today));

        var dashboard = await laporan.DashboardAsync();
        Assert.False(dashboard.Success);

        var jual = _sp.GetRequiredService<IPenjualanService>();
        await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(indomie, 1)], 5000));

        var ringkasKasir = await laporan.RingkasanKasirHariIniAsync();
        Assert.Equal(1, ringkasKasir.Value!.TransaksiHariIni);
        Assert.Equal(4000, ringkasKasir.Value.OmzetHariIni);
    }

    public void Dispose()
    {
        _sp.Dispose();
        _conn.Dispose();
    }
}
