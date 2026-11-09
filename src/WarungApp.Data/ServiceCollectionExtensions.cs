using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;
using WarungApp.Core.Services;

namespace WarungApp.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddWarungData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));
        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite(connectionString));
        services.AddSingleton<CurrentUserContext>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserContext>());
        services.AddSingleton<IAuthService, AuthService>();
        services.AddScoped<IBarangService, BarangService>();
        services.AddScoped<IStokService, StokService>();
        services.AddScoped<IPenjualanService, PenjualanService>();
        services.AddScoped<ILaporanService, LaporanService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMigrationService, MigrationService>();
        return services;
    }
}
