using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public partial class LaporanViewModel(ILaporanService laporan) : ObservableObject
{
    public ObservableCollection<BarangTerlarisDto> Terlaris { get; } = [];
    public ObservableCollection<BarangTidakLakuDto> TidakLaku { get; } = [];

    [ObservableProperty] private DateTime? _dari = DateTime.Today.AddDays(-6);
    [ObservableProperty] private DateTime? _sampai = DateTime.Today;
    [ObservableProperty] private long _omzet;
    [ObservableProperty] private long _laba;
    [ObservableProperty] private int _jumlahTransaksi;
    [ObservableProperty] private long _nilaiStok;
    [ObservableProperty] private string _pesan = "";

    public string OmzetTampil => Uang.Format(Omzet);
    public string LabaTampil => Uang.Format(Laba);
    public string NilaiStokTampil => Uang.Format(NilaiStok);

    partial void OnOmzetChanged(long value) => OnPropertyChanged(nameof(OmzetTampil));
    partial void OnLabaChanged(long value) => OnPropertyChanged(nameof(LabaTampil));
    partial void OnNilaiStokChanged(long value) => OnPropertyChanged(nameof(NilaiStokTampil));

    public async Task MuatAsync()
    {
        var dari = Dari ?? DateTime.Today;
        var sampai = Sampai ?? DateTime.Today;
        if (sampai < dari)
        {
            Pesan = "Tanggal 'sampai' tidak boleh sebelum 'dari'.";
            return;
        }

        Pesan = "";
        var ringkasan = await laporan.RingkasanAsync(dari, sampai);
        if (!ringkasan.Success)
        {
            Pesan = ringkasan.Error!;
            return;
        }

        Omzet = ringkasan.Value!.Omzet;
        Laba = ringkasan.Value.LabaKotor;
        JumlahTransaksi = ringkasan.Value.JumlahTransaksi;
        NilaiStok = ringkasan.Value.NilaiStok;

        var terlaris = await laporan.TerlarisAsync(dari, sampai);
        Terlaris.Clear();
        foreach (var t in terlaris.Value ?? []) Terlaris.Add(t);

        var tidakLaku = await laporan.TidakLakuAsync(dari, sampai);
        TidakLaku.Clear();
        foreach (var t in tidakLaku.Value ?? []) TidakLaku.Add(t);
    }

    [RelayCommand]
    private Task Muat() => MuatAsync();

    [RelayCommand]
    private Task PeriodeHarian() => AturPeriode(0);

    [RelayCommand]
    private Task PeriodeMingguan() => AturPeriode(6);

    [RelayCommand]
    private Task PeriodeBulanan() => AturPeriode(29);

    private Task AturPeriode(int mundurHari)
    {
        Sampai = DateTime.Today;
        Dari = DateTime.Today.AddDays(-mundurHari);
        return MuatAsync();
    }
}
