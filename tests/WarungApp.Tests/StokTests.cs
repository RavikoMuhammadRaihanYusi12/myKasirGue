using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;
using WarungApp.Data;
using Xunit;

namespace WarungApp.Tests;

public class StokTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ServiceProvider _sp;

    public StokTests()
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
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        _sp = services.BuildServiceProvider();
        using var scope = _sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    private async Task<int> SiapkanBarangAsync()
    {
        var auth = _sp.GetRequiredService<IAuthService>();
        await auth.SetupOwnerAsync("pemilik", "Budi", "rahasia123");
        await auth.LoginAsync("pemilik", "rahasia123", WarungApp.Core.Enums.Role.Pemilik);
        var barang = _sp.GetRequiredService<IBarangService>();
        var kat = await barang.SimpanKategoriAsync(null, "Makanan");
        var b = await barang.SimpanAsync(null, new BarangInput("A1", "Indomie", kat.Value, "pcs", 3000, 3500, 5));
        return b.Value!.Id;
    }

    [Fact]
    public async Task StokMasuk_MenambahStok_Dan_Mutasi()
    {
        var id = await SiapkanBarangAsync();
        var stok = _sp.GetRequiredService<IStokService>();

        var hasil = await stok.SimpanStokMasukAsync(new StokMasukInput(DateTime.Today, "Toko A", null, true,
            [new StokMasukItemInput(id, 10, 3100)]));
        Assert.True(hasil.Success, hasil.Error);

        var barang = _sp.GetRequiredService<IBarangService>();
        var cari = await barang.CariAsync("indomie", null, false, false);
        Assert.Equal(10, cari.Value![0].Stok);
        Assert.Equal(3100, cari.Value![0].HargaBeli);

        var kartu = await stok.KartuStokAsync(id);
        Assert.Single(kartu.Value!);
        Assert.Equal(0, kartu.Value![0].StokSebelum);
        Assert.Equal(10, kartu.Value![0].StokSesudah);
    }

    [Fact]
    public async Task StokMasuk_TanpaItem_Ditolak()
    {
        await SiapkanBarangAsync();
        var stok = _sp.GetRequiredService<IStokService>();
        var hasil = await stok.SimpanStokMasukAsync(new StokMasukInput(DateTime.Today, null, null, false, []));
        Assert.False(hasil.Success);
    }

    [Fact]
    public async Task Opname_MenyesuaikanStok_Dan_Mutasi()
    {
        var id = await SiapkanBarangAsync();
        var stok = _sp.GetRequiredService<IStokService>();
        await stok.SimpanStokMasukAsync(new StokMasukInput(DateTime.Today, null, null, false,
            [new StokMasukItemInput(id, 10, 3000)]));

        var opname = await stok.SimpanOpnameAsync(new OpnameInput(id, 7, "Rusak"));
        Assert.True(opname.Success, opname.Error);

        var cari = await _sp.GetRequiredService<IBarangService>().CariAsync("indomie", null, false, false);
        Assert.Equal(7, cari.Value![0].Stok);

        var riwayat = await stok.DaftarOpnameAsync(null, null);
        Assert.Single(riwayat.Value!);
        Assert.Equal(-3, riwayat.Value![0].Selisih);
    }

    [Fact]
    public async Task Opname_AlasanKosong_Ditolak()
    {
        var id = await SiapkanBarangAsync();
        var stok = _sp.GetRequiredService<IStokService>();
        var hasil = await stok.SimpanOpnameAsync(new OpnameInput(id, 3, " "));
        Assert.False(hasil.Success);
    }

    [Fact]
    public async Task Kasir_TidakBolehStokMasuk()
    {
        var id = await SiapkanBarangAsync();
        var auth = _sp.GetRequiredService<IAuthService>();
        await auth.TambahKasirAsync("kasir", "Siti", "rahasia123");
        _sp.GetRequiredService<ICurrentUser>().Clear();
        await auth.LoginAsync("kasir", "rahasia123", WarungApp.Core.Enums.Role.Kasir);

        var stok = _sp.GetRequiredService<IStokService>();
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            stok.SimpanStokMasukAsync(new StokMasukInput(DateTime.Today, null, null, false,
                [new StokMasukItemInput(id, 5, 3000)])));
    }

    public void Dispose()
    {
        _sp.Dispose();
        _conn.Dispose();
    }
}
