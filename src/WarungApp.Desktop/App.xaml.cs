using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;
using WarungApp.Data;
using WarungApp.Desktop.ViewModels;

namespace WarungApp.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled UI exception");
            MessageBox.Show("Terjadi kesalahan tak terduga. Detail tercatat di log.",
                "WarungApp", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        Directory.CreateDirectory(DbPath.BaseFolder);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(DbPath.BaseFolder, "logs", "log-.txt"),
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSerilog(Log.Logger));
        services.AddWarungData(DbPath.ConnectionString);
        services.AddScoped<BarangViewModel>();
        services.AddScoped<KategoriViewModel>();
        services.AddScoped<StokMasukViewModel>();
        services.AddScoped<OpnameViewModel>();
        services.AddScoped<KartuStokViewModel>();
        services.AddScoped<KasirViewModel>();
        services.AddScoped<RiwayatViewModel>();
        services.AddScoped<DashboardViewModel>();
        services.AddScoped<LaporanViewModel>();
        services.AddScoped<PengaturanViewModel>();
        services.AddScoped<UserViewModel>();
        services.AddScoped<LogViewModel>();
        Services = services.BuildServiceProvider();

        try
        {
            using var scope = Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMigrationService>().MigrateAsync();
            await AutoBackupHarianAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Migrasi database gagal");
            MessageBox.Show("Gagal menyiapkan database. Detail ada di folder log.",
                "WarungApp", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        ShowLogin();
    }

    public void ShowLogin()
    {
        using var scope = Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        Window window = auth.NeedsSetup() ? new SetupWindow(auth) : new LoginWindow(auth);
        window.Show();
    }

    public void MasukKeAplikasi()
    {
        var current = Services.GetRequiredService<ICurrentUser>();
        var vi = Services.GetRequiredService<IServiceScopeFactory>();
        var main = new MainWindow();
        var vm = new MainViewModel(vi, current, () =>
        {
            // Login dibuka dulu. Kalau main ditutup duluan, WPF menganggap itu window terakhir dan mematikan app.
            current.Clear();
            ShowLogin();
            main.Close();
            Current.MainWindow = null;
        });
        main.DataContext = vm;
        Current.MainWindow = main;
        main.Show();
        _ = vm.InisialisasiAsync();

        if (current.User?.Role == Core.Enums.Role.Pemilik)
        {
            using var scope = Services.CreateScope();
            var laporan = scope.ServiceProvider.GetRequiredService<ILaporanService>();
            var menipis = laporan.JumlahStokMenipisAsync().Result;
            if (menipis.Value > 0)
                main.Dispatcher.Invoke(() => MessageBox.Show(
                    $"Ada {menipis.Value} barang dengan stok menipis. Periksa dashboard.",
                    "Peringatan Stok", MessageBoxButton.OK, MessageBoxImage.Warning));
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<IBackupService>().BackupAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Backup saat keluar gagal");
        }

        (Services as IDisposable)?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static async Task AutoBackupHarianAsync()
    {
        using var scope = Services.CreateScope();
        var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
        var daftar = await backup.DaftarBackupAsync();
        var hariIni = $"-{DateTime.Now:yyyyMMdd}";
        if (daftar.Any(f => Path.GetFileName(f).Contains(hariIni))) return;

        var hasil = await backup.BackupAsync();
        if (!hasil.Success) Log.Warning("Auto-backup harian gagal: {Error}", hasil.Error);
    }
}
