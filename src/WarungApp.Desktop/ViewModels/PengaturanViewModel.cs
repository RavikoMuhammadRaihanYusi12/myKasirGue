using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public partial class PengaturanViewModel(
    ISettingsService settings,
    IBackupService backup,
    IAuditService audit) : ObservableObject
{
    public ObservableCollection<string> DaftarBackup { get; } = [];

    [ObservableProperty] private string _namaToko = "";
    [ObservableProperty] private string _alamat = "";
    [ObservableProperty] private string _telepon = "";
    [ObservableProperty] private bool _bolehStokMinus;
    [ObservableProperty] private string _folderBackup = "";
    [ObservableProperty] private string _retensiBackup = "14";
    [ObservableProperty] private string _pesan = "";
    [ObservableProperty] private string _pesanBackup = "";

    public string FolderAktif { get; private set; } = "";

    public async Task MuatAsync()
    {
        var s = await settings.AmbilAsync();
        NamaToko = s.NamaToko;
        Alamat = s.Alamat;
        Telepon = s.Telepon;
        BolehStokMinus = s.BolehStokMinus;
        FolderBackup = s.FolderBackup;
        RetensiBackup = s.RetensiBackup.ToString();
        await MuatBackup();
    }

    private async Task MuatBackup()
    {
        FolderAktif = await backup.FolderBackupAktifAsync();
        var daftar = await backup.DaftarBackupAsync();
        DaftarBackup.Clear();
        foreach (var f in daftar) DaftarBackup.Add(System.IO.Path.GetFileName(f));
    }

    [RelayCommand]
    private async Task Simpan()
    {
        if (!int.TryParse(RetensiBackup, out var retensi) || retensi < 1)
        {
            Pesan = "Retensi backup harus angka minimal 1.";
            return;
        }

        var hasil = await settings.SimpanAsync(new PengaturanDto(NamaToko, Alamat, Telepon, BolehStokMinus,
            FolderBackup, retensi));
        Pesan = hasil.Success ? "Pengaturan tersimpan." : hasil.Error!;
        if (hasil.Success) await MuatBackup();
    }

    [RelayCommand]
    private void PilihFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Pilih folder backup" };
        if (dialog.ShowDialog() == true) FolderBackup = dialog.FolderName;
    }

    [RelayCommand]
    private async Task BackupSekarang()
    {
        var hasil = await backup.BackupAsync(string.IsNullOrWhiteSpace(FolderBackup) ? null : FolderBackup);
        PesanBackup = hasil.Success ? $"Backup dibuat: {System.IO.Path.GetFileName(hasil.Value!)}" : hasil.Error!;
        if (hasil.Success) await MuatBackup();
    }

    [RelayCommand]
    private async Task Restore(string? namaFile)
    {
        if (string.IsNullOrWhiteSpace(namaFile))
        {
            PesanBackup = "Pilih file backup dari daftar.";
            return;
        }

        var konfirmasi = System.Windows.MessageBox.Show(
            "Data saat ini akan DITIMPA dengan isi backup. Lanjutkan?",
            "Restore Backup", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (konfirmasi != System.Windows.MessageBoxResult.Yes) return;

        var path = System.IO.Path.Combine(FolderAktif, namaFile);
        var hasil = await backup.RestoreAsync(path);
        PesanBackup = hasil.Success
            ? "Restore berhasil. Tutup dan buka ulang aplikasi agar data termuat."
            : hasil.Error!;
        if (hasil.Success)
        {
            try { await audit.CatatAsync("Restore backup", namaFile); } catch { }
        }
    }
}
