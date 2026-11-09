using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public partial class KategoriViewModel(IBarangService barang, Func<Task>? setelahSimpan = null) : ObservableObject
{
    public ObservableCollection<KategoriDto> Daftar { get; } = [];

    [ObservableProperty] private KategoriDto? _terpilih;
    [ObservableProperty] private string _nama = "";
    [ObservableProperty] private string _pesan = "";
    [ObservableProperty] private bool _pesanError;

    public async Task MuatAsync()
    {
        var hasil = await barang.DaftarKategoriAsync();
        Daftar.Clear();
        foreach (var k in hasil.Value ?? []) Daftar.Add(k);
    }

    [RelayCommand]
    private void Baru()
    {
        Terpilih = null;
        Nama = "";
        Pesan = "";
    }

    [RelayCommand]
    private void Pilih(KategoriDto? dto)
    {
        Terpilih = dto;
        Nama = dto?.Nama ?? "";
        Pesan = "";
    }

    [RelayCommand]
    private async Task Simpan()
    {
        var hasil = await barang.SimpanKategoriAsync(Terpilih?.Id, Nama);
        if (!hasil.Success)
        {
            Pesan = hasil.Error!;
            PesanError = true;
            return;
        }

        Pesan = "Kategori tersimpan.";
        PesanError = false;
        await MuatAsync();
        if (setelahSimpan is not null) await setelahSimpan();
    }
}
