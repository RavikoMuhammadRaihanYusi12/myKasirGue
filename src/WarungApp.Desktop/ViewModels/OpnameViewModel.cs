using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public partial class OpnameItemVm(BarangDto barang) : ObservableObject
{
    public BarangDto Barang { get; } = barang;
    public string Nama => Barang.Nama;
    public int StokSistem => Barang.Stok;
    public string Satuan => Barang.Satuan;

    [ObservableProperty] private string _stokFisik = "";
    [ObservableProperty] private string _alasan = "Koreksi input";

    public int Selisih => int.TryParse(StokFisik, out var f) ? f - StokSistem : 0;

    public bool StokFisikValid => int.TryParse(StokFisik, out var f) && f >= 0 && !string.IsNullOrWhiteSpace(Alasan);

    partial void OnStokFisikChanged(string value)
    {
        OnPropertyChanged(nameof(Selisih));
        OnPropertyChanged(nameof(StokFisikValid));
    }

    partial void OnAlasanChanged(string value) => OnPropertyChanged(nameof(StokFisikValid));
}

public partial class OpnameViewModel(IStokService stok, IBarangService barang) : ObservableObject
{
    public ObservableCollection<OpnameItemVm> Baris { get; } = [];
    public ObservableCollection<PenyesuaianDto> Riwayat { get; } = [];
    public ObservableCollection<string> AlasanPilihan { get; } =
        ["Rusak", "Hilang", "Kadaluarsa", "Koreksi input", "Lainnya"];

    public ObservableCollection<BarangDto> HasilCari { get; } = [];

    [ObservableProperty] private string _kataCari = "";
    [ObservableProperty] private BarangDto? _hasilTerpilih;
    [ObservableProperty] private string _pesan = "";
    [ObservableProperty] private bool _pesanError;

    public async Task MuatAsync() => await MuatRiwayat();

    [RelayCommand]
    private async Task CariBarang()
    {
        var hasil = await barang.CariAsync(KataCari, null, false, false);
        HasilCari.Clear();
        foreach (var b in hasil.Value ?? []) HasilCari.Add(b);
    }

    [RelayCommand]
    private void TambahBaris()
    {
        if (HasilTerpilih is null)
        {
            SetPesan("Pilih barang terlebih dahulu.", true);
            return;
        }

        if (Baris.Any(b => b.Barang.Id == HasilTerpilih.Id))
        {
            SetPesan("Barang sudah ada di daftar opname.", true);
            return;
        }

        Baris.Add(new OpnameItemVm(HasilTerpilih));
        HasilTerpilih = null;
        KataCari = "";
        SetPesan("", false);
    }

    [RelayCommand]
    private void HapusBaris(OpnameItemVm? item)
    {
        if (item is not null) Baris.Remove(item);
    }

    [RelayCommand]
    private async Task Simpan()
    {
        var valid = Baris.Where(b => b.StokFisikValid).ToList();
        if (valid.Count == 0)
        {
            SetPesan("Isi stok fisik minimal satu barang.", true);
            return;
        }

        var gagal = new List<string>();
        foreach (var b in valid)
        {
            var hasil = await stok.SimpanOpnameAsync(new OpnameInput(b.Barang.Id, int.Parse(b.StokFisik), b.Alasan));
            if (!hasil.Success) gagal.Add($"{b.Nama}: {hasil.Error}");
        }

        SetPesan(gagal.Count == 0 ? "Opname tersimpan." : string.Join("\n", gagal), gagal.Count > 0);
        if (gagal.Count == 0) Baris.Clear();
        await MuatRiwayat();
        await CariBarang();
    }

    private async Task MuatRiwayat()
    {
        var hasil = await stok.DaftarOpnameAsync(null, null);
        Riwayat.Clear();
        foreach (var r in hasil.Value ?? []) Riwayat.Add(r);
    }

    private void SetPesan(string pesan, bool error)
    {
        Pesan = pesan;
        PesanError = error;
    }
}
