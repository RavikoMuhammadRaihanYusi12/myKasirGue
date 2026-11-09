using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;
using WarungApp.Data;
using Xunit;

namespace WarungApp.Tests;

public class AuthTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ServiceProvider _sp;

    public AuthTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_conn));
        services.AddSingleton<CurrentUserContext>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserContext>());
        services.AddSingleton<IAuthService, WarungApp.Core.Services.AuthService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMigrationService, MigrationService>();
        _sp = services.BuildServiceProvider();
        using var scope = _sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    private IAuthService Auth => _sp.GetRequiredService<IAuthService>();

    [Fact]
    public async Task SetupOwner_Lalu_Login_Berhasil()
    {
        Assert.True(Auth.NeedsSetup());

        var setup = await Auth.SetupOwnerAsync("pemilik", "Budi", "rahasia123");
        Assert.True(setup.Success, setup.Error);
        Assert.False(Auth.NeedsSetup());

        var login = await Auth.LoginAsync("pemilik", "rahasia123", WarungApp.Core.Enums.Role.Pemilik);
        Assert.True(login.Success, login.Error);
        Assert.Equal(WarungApp.Core.Enums.Role.Pemilik, login.Value!.Role);
    }

    [Fact]
    public async Task Login_PasswordSalah_Gagal()
    {
        await Auth.SetupOwnerAsync("pemilik", "Budi", "rahasia123");
        var login = await Auth.LoginAsync("pemilik", "salah", WarungApp.Core.Enums.Role.Pemilik);
        Assert.False(login.Success);
    }

    [Fact]
    public async Task Setup_KeduaKali_Ditolak()
    {
        await Auth.SetupOwnerAsync("pemilik", "Budi", "rahasia123");
        var second = await Auth.SetupOwnerAsync("pemilik2", "Ani", "rahasia123");
        Assert.False(second.Success);
    }

    public void Dispose()
    {
        _sp.Dispose();
        _conn.Dispose();
    }
}
