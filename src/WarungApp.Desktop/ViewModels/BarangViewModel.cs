using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public partial class BarangViewModel(IBarangService barang, Func<Task>? mintaRefresh = null) : ObservableObject
{
    public ObservableCollection<BarangDto> Daftar { get; } = [];
    public ObservableCollection<KategoriDto> KategoriList { get; } = [];

    [ObservableProperty] private string _kataCari = "";
    [ObservableProperty] private KategoriDto? _kategoriTerpilih;
    [ObservableProperty] private bool _hanyaMenipis;
    [ObservableProperty] private bool _tampilkanNonaktif;
    [ObservableProperty] private string _pesan = "";
    [ObservableProperty] private bool _pesanError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(JudulForm), nameof(BisiNonaktifkan), nameof(BisiAktifkan))]
    private BarangDto? _terpilihBarang;

    public string JudulForm => TerpilihBarang is null ? "Tambah Barang Baru" : $"Ubah: {TerpilihBarang.Nama}";
    public bool BisiNonaktifkan => TerpilihBarang?.Aktif == true;
    public bool BisiAktifkan => TerpilihBarang?.Aktif == false;

    [ObservableProperty] private string _formKode = "";
    [ObservableProperty] private string _formNama = "";
    [ObservableProperty] private KategoriDto? _formKategori;
    [ObservableProperty] private string _formSatuan = "pcs";
    [ObservableProperty] private string _formHargaBeli = "0";
    [ObservableProperty] private string _formHargaJual = "0";
    [ObservableProperty] private string _formStokMinimum = "0";
    [ObservableProperty] private bool _formAktif = true;

    public async Task MuatAsync()
    {
        var kat = await barang.DaftarKategoriAsync();
        KategoriList.Clear();
        foreach (var k in kat.Value ?? []) KategoriList.Add(k);

        var hasil = await barang.CariAsync(KataCari, KategoriTerpilih?.Id, HanyaMenipis, TampilkanNonaktif);
        Daftar.Clear();
        foreach (var b in hasil.Value ?? []) Daftar.Add(b);
    }

    [RelayCommand]
    private Task Cari() => MuatAsync();

    [RelayCommand]
    private void Baru()
    {
        TerpilihBarang = null;
        Pesan = "";
        FormKode = "";
        FormNama = "";
        FormKategori = KategoriList.FirstOrDefault();
        FormSatuan = "pcs";
        FormHargaBeli = "0";
        FormHargaJual = "0";
        FormStokMinimum = "0";
        FormAktif = true;
    }

    [RelayCommand]
    private void Pilih(BarangDto? dto)
    {
        if (dto is null) return;
        TerpilihBarang = dto;
        FormKode = dto.Kode ?? "";
        FormNama = dto.Nama;
        FormKategori = KategoriList.FirstOrDefault(k => k.Id == dto.KategoriId);
        FormSatuan = dto.Satuan;
        FormHargaBeli = dto.HargaBeli.ToString();
        FormHargaJual = dto.HargaJual.ToString();
        FormStokMinimum = dto.StokMinimum.ToString();
        FormAktif = dto.Aktif;
        Pesan = "";
    }

    [RelayCommand]
    private async Task Simpan()
    {
        if (FormKategori is null)
        {
            SetPesan("Pilih kategori terlebih dahulu.", true);
            return;
        }

        if (!Core.Common.Uang.TryParse(FormHargaBeli, out var beli) ||
            !Core.Common.Uang.TryParse(FormHargaJual, out var jual) ||
            !int.TryParse(FormStokMinimum, out var stokMin))
        {
            SetPesan("Harga dan stok minimum harus berupa angka.", true);
            return;
        }

        var input = new BarangInput(FormKode, FormNama, FormKategori.Id, FormSatuan, beli, jual, stokMin, FormAktif);
        var hasil = await barang.SimpanAsync(TerpilihBarang?.Id, input);
        if (!hasil.Success)
        {
            SetPesan(hasil.Error!, true);
            return;
        }

        SetPesan(hasil.Value!.HargaJual < hasil.Value.HargaBeli
            ? $"Tersimpan. Peringatan: harga jual di bawah harga beli."
            : "Tersimpan.", hasil.Value.HargaJual < hasil.Value.HargaBeli);

        TerpilihBarang = hasil.Value;
        await MuatAsync();
        if (mintaRefresh is not null) await mintaRefresh();
    }

    [RelayCommand]
    private Task Nonaktifkan() => UbahStatusAsync(false);

    [RelayCommand]
    private Task Aktifkan() => UbahStatusAsync(true);

    private async Task UbahStatusAsync(bool aktif)
    {
        if (TerpilihBarang is null) return;
        var id = TerpilihBarang.Id;
        var hasil = aktif ? await barang.AktifkanAsync(id) : await barang.NonaktifkanAsync(id);
        if (!hasil.Success)
        {
            SetPesan(hasil.Error!, true);
            return;
        }

        await MuatAsync();
        // Muat ulang status terpilih; kalau barang tidak lagi tampil (filter nonaktif), kosongkan form.
        TerpilihBarang = Daftar.FirstOrDefault(b => b.Id == id);
        if (TerpilihBarang is null) Baru();
        SetPesan(aktif ? "Barang diaktifkan." : "Barang dinonaktifkan.", false);
        if (mintaRefresh is not null) await mintaRefresh();
    }

    private void SetPesan(string pesan, bool error)
    {
        Pesan = pesan;
        PesanError = error;
    }
}
