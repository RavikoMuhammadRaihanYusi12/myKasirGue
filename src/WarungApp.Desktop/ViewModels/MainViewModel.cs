using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public record MenuItem(string Kunci, string Judul, bool PemilikSaja);

public partial class MainViewModel(IServiceScopeFactory scopes, ICurrentUser current, Action logout) : ObservableObject
{
    public string NamaUser => current.User?.NamaLengkap ?? "";
    public string NamaRole => current.User?.Role.ToString() ?? "";
    public bool IsPemilik => current.User?.Role == Core.Enums.Role.Pemilik;

    public List<MenuItem> Menu { get; } = BuildMenu(current);

    [ObservableProperty] private object? _kontenAktif;
    [ObservableProperty] private string _judulHalaman = "";
    [ObservableProperty] private string _menuAktif = "";
    [ObservableProperty] private MenuItem? _menuTerpilih;

    public Task InisialisasiAsync()
    {
        MenuTerpilih = Menu.First();
        return BukaAsync(MenuTerpilih);
    }

    [RelayCommand]
    private Task BukaMenu(MenuItem? item) => item is null ? Task.CompletedTask : BukaAsync(item);

    [RelayCommand]
    private void Logout() => logout();

    private Task BukaAsync(MenuItem item)
    {
        MenuAktif = item.Kunci;
        JudulHalaman = item.Judul;
        KontenAktif = item.Kunci switch
        {
            "dashboard" => Buat<Views.DashboardView, DashboardViewModel>(),
            "laporan" => Buat<Views.LaporanView, LaporanViewModel>(),
            "kasir" => Buat<Views.KasirView, KasirViewModel>(),
            "barang" => Buat<Views.BarangView, BarangViewModel>(),
            "kategori" => Buat<Views.KategoriView, KategoriViewModel>(),
            "stokmasuk" => Buat<Views.StokMasukView, StokMasukViewModel>(),
            "opname" => Buat<Views.OpnameView, OpnameViewModel>(),
            "kartustok" => Buat<Views.KartuStokView, KartuStokViewModel>(),
            "riwayat" => Buat<Views.RiwayatView, RiwayatViewModel>(),
            "pengaturan" => Buat<Views.PengaturanView, PengaturanViewModel>(),
            "user" => Buat<Views.UserView, UserViewModel>(),
            "log" => Buat<Views.LogView, LogViewModel>(),
            _ => new System.Windows.Controls.TextBlock { Text = "Segera hadir." }
        };
        return Task.CompletedTask;
    }

    private FrameworkElement Buat<TView, TVm>() where TView : FrameworkElement, new() where TVm : notnull
    {
        var scope = scopes.CreateScope();
        return new TView { DataContext = scope.ServiceProvider.GetRequiredService<TVm>() };
    }

    private static List<MenuItem> BuildMenu(ICurrentUser current) =>
        current.User?.Role == Core.Enums.Role.Kasir
            ? [new("kasir", "Kasir", false), new("riwayat", "Transaksi Saya", false)]
            : [
                new("dashboard", "Dashboard", true),
                new("kasir", "Kasir", true),
                new("barang", "Data Barang", true),
                new("kategori", "Kategori", true),
                new("stokmasuk", "Stok Masuk", true),
                new("opname", "Stok Opname", true),
                new("kartustok", "Kartu Stok", true),
                new("riwayat", "Riwayat & Void", true),
                new("laporan", "Laporan", true),
                new("user", "Pengguna", true),
                new("pengaturan", "Pengaturan & Backup", true),
                new("log", "Log Aktivitas", true)
            ];
}
