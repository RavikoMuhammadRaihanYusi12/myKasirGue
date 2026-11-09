using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public partial class KartuStokViewModel(IStokService stok, IBarangService barang) : ObservableObject
{
    public ObservableCollection<BarangDto> HasilCari { get; } = [];
    public ObservableCollection<MutasiDto> Mutasi { get; } = [];

    [ObservableProperty] private string _kataCari = "";
    [ObservableProperty] private BarangDto? _hasilTerpilih;
    [ObservableProperty] private string _judul = "Pilih barang untuk melihat kartu stok";

    [RelayCommand]
    private async Task Cari()
    {
        var hasil = await barang.CariAsync(KataCari, null, false, true);
        HasilCari.Clear();
        foreach (var b in hasil.Value ?? []) HasilCari.Add(b);
        if (HasilCari.Count == 1) await MuatKartu(HasilCari[0]);
    }

    public async Task MuatKartu(BarangDto dto)
    {
        Judul = $"Kartu stok: {dto.Nama} (stok sekarang {dto.Stok} {dto.Satuan})";
        var hasil = await stok.KartuStokAsync(dto.Id);
        Mutasi.Clear();
        foreach (var m in hasil.Value ?? []) Mutasi.Add(m);
    }

    [RelayCommand]
    private async Task Pilih(BarangDto? dto)
    {
        if (dto is null) return;
        HasilTerpilih = dto;
        await MuatKartu(dto);
    }
}
