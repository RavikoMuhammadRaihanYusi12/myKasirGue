using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;
using WarungApp.Data;
using Xunit;

namespace WarungApp.Tests;

public class PenjualanTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ServiceProvider _sp;

    public PenjualanTests()
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
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        _sp = services.BuildServiceProvider();
        using var scope = _sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    private async Task<int> SiapkanAsync(int stokAwal = 10)
    {
        var auth = _sp.GetRequiredService<IAuthService>();
        await auth.SetupOwnerAsync("pemilik", "Budi", "rahasia123");
        await auth.LoginAsync("pemilik", "rahasia123", WarungApp.Core.Enums.Role.Pemilik);

        var barang = _sp.GetRequiredService<IBarangService>();
        var kat = await barang.SimpanKategoriAsync(null, "Makanan");
        var b = await barang.SimpanAsync(null, new BarangInput("A1", "Indomie", kat.Value, "pcs", 3000, 3500, 5));

        var stok = _sp.GetRequiredService<IStokService>();
        await stok.SimpanStokMasukAsync(new StokMasukInput(DateTime.Today, null, null, false,
            [new StokMasukItemInput(b.Value!.Id, stokAwal, 3000)]));
        return b.Value.Id;
    }

    [Fact]
    public async Task BuatPenjualan_MengurangiStok_Dan_Mutasi()
    {
        var id = await SiapkanAsync();
        var jual = _sp.GetRequiredService<IPenjualanService>();

        var hasil = await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(id, 3)], 20000));
        Assert.True(hasil.Success, hasil.Error);
        Assert.Equal(10500, hasil.Value!.Total);
        Assert.Equal(9500, hasil.Value.Kembalian);
        Assert.Equal(1500, hasil.Value.Laba);
        Assert.StartsWith("TRX-", hasil.Value.NoTransaksi);

        var cari = await _sp.GetRequiredService<IBarangService>().CariAsync("indomie", null, false, false);
        Assert.Equal(7, cari.Value![0].Stok);

        var kartu = await _sp.GetRequiredService<IStokService>().KartuStokAsync(id);
        Assert.Equal(2, kartu.Value!.Count);
        Assert.Equal(-3, kartu.Value![0].Qty);
    }

    [Fact]
    public async Task StokKurang_Diblokir_DanStokTidakBerubah()
    {
        var id = await SiapkanAsync(2);
        var jual = _sp.GetRequiredService<IPenjualanService>();

        await Assert.ThrowsAsync<AppException>(() =>
            jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(id, 5)], 50000)));

        var cari = await _sp.GetRequiredService<IBarangService>().CariAsync("indomie", null, false, false);
        Assert.Equal(2, cari.Value![0].Stok);
        Assert.Empty(_sp.GetRequiredService<AppDbContext>().Penjualan.ToList());
    }

    [Fact]
    public async Task BayarKurang_Ditolak_DanStokTidakBerubah()
    {
        var id = await SiapkanAsync();
        var jual = _sp.GetRequiredService<IPenjualanService>();

        await Assert.ThrowsAsync<AppException>(() =>
            jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(id, 2)], 1000)));

        var cari = await _sp.GetRequiredService<IBarangService>().CariAsync("indomie", null, false, false);
        Assert.Equal(10, cari.Value![0].Stok);
    }

    [Fact]
    public async Task SnapshotHarga_TidakBerubahSaatHargaBarangDiubah()
    {
        var id = await SiapkanAsync();
        var jual = _sp.GetRequiredService<IPenjualanService>();
        var hasil = await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(id, 1)], 5000));

        var barang = _sp.GetRequiredService<IBarangService>();
        await barang.SimpanAsync(id, new BarangInput("A1", "Indomie", 1, "pcs", 3000, 9000, 5));

        var detail = await jual.DetailAsync(hasil.Value!.Id);
        Assert.Equal(3500, detail.Value!.Items[0].HargaJual);
        Assert.Equal(3500, detail.Value.Total);
    }

    [Fact]
    public async Task Void_MengembalikanStok_Dan_StatusVoid()
    {
        var id = await SiapkanAsync();
        var jual = _sp.GetRequiredService<IPenjualanService>();
        var hasil = await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(id, 4)], 20000));

        var voidHasil = await jual.VoidAsync(hasil.Value!.Id, "Salah input");
        Assert.True(voidHasil.Success, voidHasil.Error);

        var cari = await _sp.GetRequiredService<IBarangService>().CariAsync("indomie", null, false, false);
        Assert.Equal(10, cari.Value![0].Stok);

        var daftar = await jual.DaftarAsync(null, null, false);
        Assert.Equal("Void", daftar.Value![0].Status);
        Assert.Equal("Salah input", daftar.Value[0].AlasanVoid);
    }

    [Fact]
    public async Task Void_TanpaAlasan_Ditolak()
    {
        var id = await SiapkanAsync();
        var jual = _sp.GetRequiredService<IPenjualanService>();
        var hasil = await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(id, 1)], 5000));

        var voidHasil = await jual.VoidAsync(hasil.Value!.Id, "");
        Assert.False(voidHasil.Success);
    }

    [Fact]
    public async Task Kasir_TidakBolehVoid_Dan_HanyaLihatMiliknya()
    {
        var id = await SiapkanAsync();
        var jual = _sp.GetRequiredService<IPenjualanService>();
        var auth = _sp.GetRequiredService<IAuthService>();

        var ownerTrx = await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(id, 1)], 5000));

        await auth.TambahKasirAsync("kasir", "Siti", "rahasia123");
        _sp.GetRequiredService<ICurrentUser>().Clear();
        await auth.LoginAsync("kasir", "rahasia123", WarungApp.Core.Enums.Role.Kasir);

        await Assert.ThrowsAsync<ForbiddenException>(() => jual.VoidAsync(ownerTrx.Value!.Id, "coba"));

        var kasirTrx = await jual.BuatPenjualanAsync(new PenjualanInput([new PenjualanItemInput(id, 2)], 10000));
        Assert.True(kasirTrx.Success, kasirTrx.Error);

        var daftar = await jual.DaftarAsync(null, null, false);
        Assert.Single(daftar.Value!);
        Assert.Equal(kasirTrx.Value!.Id, daftar.Value![0].Id);

        var detailOrangLain = await jual.DetailAsync(ownerTrx.Value!.Id);
        Assert.False(detailOrangLain.Success);
    }

    public void Dispose()
    {
        _sp.Dispose();
        _conn.Dispose();
    }
}
