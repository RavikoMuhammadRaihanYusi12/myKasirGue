using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public partial class DashboardViewModel(ILaporanService laporan) : ObservableObject
{
    public ObservableCollection<StokMenipisDto> StokMenipis { get; } = [];
    public ObservableCollection<BarangTerlarisDto> Terlaris { get; } = [];

    [ObservableProperty] private long _omzet;
    [ObservableProperty] private long _laba;
    [ObservableProperty] private int _jumlahTransaksi;
    [ObservableProperty] private int _jumlahMenipis;

    public string OmzetTampil => Uang.Format(Omzet);
    public string LabaTampil => Uang.Format(Laba);

    partial void OnOmzetChanged(long value) => OnPropertyChanged(nameof(OmzetTampil));
    partial void OnLabaChanged(long value) => OnPropertyChanged(nameof(LabaTampil));

    public async Task MuatAsync()
    {
        var hasil = await laporan.DashboardAsync();
        if (!hasil.Success) return;

        var d = hasil.Value!;
        Omzet = d.OmzetHariIni;
        Laba = d.LabaHariIni;
        JumlahTransaksi = d.TransaksiHariIni;
        JumlahMenipis = d.JumlahStokMenipis;

        StokMenipis.Clear();
        foreach (var s in d.StokMenipis) StokMenipis.Add(s);

        Terlaris.Clear();
        foreach (var t in d.TerlarisHariIni) Terlaris.Add(t);
    }

    [RelayCommand]
    private Task Muat() => MuatAsync();
}
