using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;
using WarungApp.Data;
using Xunit;

namespace WarungApp.Tests;

public class BarangTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ServiceProvider _sp;

    public BarangTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_conn));
        services.AddSingleton<CurrentUserContext>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserContext>());
        services.AddSingleton<IAuthService, WarungApp.Core.Services.AuthService>();
        services.AddScoped<IBarangService, WarungApp.Core.Services.BarangService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMigrationService, MigrationService>();
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
    public async Task SimpanBarang_Lalu_Cari_Menemukan()
    {
        await LoginPemilikAsync();
        var svc = _sp.GetRequiredService<IBarangService>();
        var kat = await svc.SimpanKategoriAsync(null, "Minuman");
        Assert.True(kat.Success, kat.Error);

        var simpan = await svc.SimpanAsync(null, new BarangInput("BRG-1", "Teh Botol", kat.Value, "pcs", 3000, 5000, 5));
        Assert.True(simpan.Success, simpan.Error);

        var cari = await svc.CariAsync("teh", null, false, false);
        Assert.Single(cari.Value!);
        Assert.Equal("Teh Botol", cari.Value![0].Nama);
    }

    [Fact]
    public async Task KodeDuplikat_Ditolak()
    {
        await LoginPemilikAsync();
        var svc = _sp.GetRequiredService<IBarangService>();
        var kat = await svc.SimpanKategoriAsync(null, "Snack");
        await svc.SimpanAsync(null, new BarangInput("X1", "Keripik", kat.Value, "pcs", 1000, 2000, 1));
        var kedua = await svc.SimpanAsync(null, new BarangInput("X1", "Kerupuk", kat.Value, "pcs", 1000, 2000, 1));
        Assert.False(kedua.Success);
    }

    [Fact]
    public async Task Kasir_TidakBolehUbahBarang()
    {
        await LoginPemilikAsync();
        var svc = _sp.GetRequiredService<IBarangService>();
        var auth = _sp.GetRequiredService<IAuthService>();
        var kat = await svc.SimpanKategoriAsync(null, "Rokok");

        var tambah = await auth.TambahKasirAsync("kasir1", "Siti", "rahasia123");
        Assert.True(tambah.Success, tambah.Error);

        var current = _sp.GetRequiredService<ICurrentUser>();
        current.Clear();
        await auth.LoginAsync("kasir1", "rahasia123", WarungApp.Core.Enums.Role.Kasir);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.SimpanAsync(null, new BarangInput(null, "Gula", kat.Value, "kg", 10000, 14000, 2)));

        var kasirCari = await svc.CariUntukKasirAsync("gula");
        Assert.True(kasirCari.Success);
    }

    [Fact]
    public async Task Nonaktifkan_Barang_TidakMunculDiCariDefault()
    {
        await LoginPemilikAsync();
        var svc = _sp.GetRequiredService<IBarangService>();
        var kat = await svc.SimpanKategoriAsync(null, "Lain");
        var b = await svc.SimpanAsync(null, new BarangInput(null, "Sabun", kat.Value, "pcs", 3000, 4000, 1));
        await svc.NonaktifkanAsync(b.Value!.Id);

        var biasa = await svc.CariAsync("sabun", null, false, false);
        Assert.Empty(biasa.Value!);
        var semua = await svc.CariAsync("sabun", null, false, true);
        Assert.Single(semua.Value!);
        Assert.False(semua.Value![0].Aktif);
    }

    public void Dispose()
    {
        _sp.Dispose();
        _conn.Dispose();
    }
}
