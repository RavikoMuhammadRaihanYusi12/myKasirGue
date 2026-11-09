using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public partial class KeranjangMasukItem(BarangDto barang) : ObservableObject
{
    public int BarangId => barang.Id;
    public string Nama => barang.Nama;
    public string Satuan => barang.Satuan;

    [ObservableProperty] private string _qty = "1";
    [ObservableProperty] private string _hargaBeli = "0";

    public long Subtotal =>
        (Uang.TryParse(Qty, out var q) ? q : 0) * (Uang.TryParse(HargaBeli, out var h) ? h : 0);

    public event Action? Berubah;

    partial void OnQtyChanged(string value) => Berubah?.Invoke();
    partial void OnHargaBeliChanged(string value) => Berubah?.Invoke();
}

public partial class StokMasukViewModel(IStokService stok, IBarangService barang) : ObservableObject
{
    public ObservableCollection<BarangDto> HasilCari { get; } = [];
    public ObservableCollection<KeranjangMasukItem> Keranjang { get; } = [];
    public ObservableCollection<StokMasukDto> Riwayat { get; } = [];

    [ObservableProperty] private string _kataCari = "";
    [ObservableProperty] private BarangDto? _hasilTerpilih;
    [ObservableProperty] private string _inputQty = "1";
    [ObservableProperty] private DateTime _tanggal = DateTime.Today;
    [ObservableProperty] private string _supplier = "";
    [ObservableProperty] private string _catatan = "";
    [ObservableProperty] private bool _updateHargaBeli = true;
    [ObservableProperty] private string _pesan = "";
    [ObservableProperty] private bool _pesanError;

    public string TotalTampil => Uang.Format(Keranjang.Sum(k => k.Subtotal));

    public async Task MuatAsync()
    {
        await MuatRiwayat();
        await CariBarang();
    }

    [RelayCommand]
    private async Task CariBarang()
    {
        var hasil = await barang.CariAsync(KataCari, null, false, false);
        HasilCari.Clear();
        foreach (var b in hasil.Value ?? []) HasilCari.Add(b);
    }

    [RelayCommand]
    private void TambahItem()
    {
        if (HasilTerpilih is null)
        {
            SetPesan("Pilih barang dari daftar pencarian.", true);
            return;
        }

        if (!Uang.TryParse(InputQty, out var inputQty) || inputQty <= 0)
        {
            SetPesan("Qty tidak valid.", true);
            return;
        }

        var ada = Keranjang.FirstOrDefault(k => k.BarangId == HasilTerpilih.Id);
        if (ada is not null)
        {
            Uang.TryParse(ada.Qty, out var q);
            ada.Qty = (q + inputQty).ToString();
        }
        else
        {
            var baru = new KeranjangMasukItem(HasilTerpilih) 
            { 
                HargaBeli = HasilTerpilih.HargaBeli.ToString(),
                Qty = inputQty.ToString()
            };
            baru.Berubah += PemicuTotal;
            Keranjang.Add(baru);
        }

        HasilTerpilih = null;
        KataCari = "";
        InputQty = "1";
        SetPesan("", false);
        PemicuTotal();
    }

    [RelayCommand]
    private void HapusItem(KeranjangMasukItem? item)
    {
        if (item is null) return;
        Keranjang.Remove(item);
        PemicuTotal();
    }

    [RelayCommand]
    private async Task Simpan()
    {
        if (Keranjang.Count == 0)
        {
            SetPesan("Keranjang masih kosong.", true);
            return;
        }

        var items = new List<StokMasukItemInput>();
        foreach (var k in Keranjang)
        {
            if (!Uang.TryParse(k.Qty, out var qty) || qty <= 0)
            {
                SetPesan($"Qty untuk {k.Nama} tidak valid.", true);
                return;
            }

            if (!Uang.TryParse(k.HargaBeli, out var harga))
            {
                SetPesan($"Harga beli untuk {k.Nama} tidak valid.", true);
                return;
            }

            items.Add(new StokMasukItemInput(k.BarangId, (int)qty, harga));
        }

        var input = new StokMasukInput(Tanggal, Supplier, Catatan, UpdateHargaBeli, items);
        var hasil = await stok.SimpanStokMasukAsync(input);
        if (!hasil.Success)
        {
            SetPesan(hasil.Error!, true);
            return;
        }

        SetPesan("Stok masuk tersimpan.", false);
        Keranjang.Clear();
        Supplier = "";
        Catatan = "";
        PemicuTotal();
        await MuatRiwayat();
        await CariBarang();
    }

    private async Task MuatRiwayat()
    {
        var hasil = await stok.DaftarStokMasukAsync(null, null);
        Riwayat.Clear();
        foreach (var r in hasil.Value ?? []) Riwayat.Add(r);
    }

    private void PemicuTotal() => OnPropertyChanged(nameof(TotalTampil));

    private void SetPesan(string pesan, bool error)
    {
        Pesan = pesan;
        PesanError = error;
    }
}
