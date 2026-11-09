using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public partial class KeranjangItem(BarangKasirDto barang) : ObservableObject
{
    public int BarangId => barang.Id;
    public string Nama => barang.Nama;
    public string Satuan => barang.Satuan;
    public long HargaJual => barang.HargaJual;
    public int StokTersedia => barang.Stok;

    [ObservableProperty] private int _qty = 1;

    public long Subtotal => HargaJual * Qty;

    public event Action? Berubah;

    partial void OnQtyChanged(int value) => Berubah?.Invoke();
}

public record TransaksiSuksesInfo(string NoTransaksi, long Total, long Bayar, long Kembalian);

public partial class KasirViewModel(IPenjualanService penjualan, IBarangService barang) : ObservableObject
{
    public ObservableCollection<BarangKasirDto> HasilCari { get; } = [];
    public ObservableCollection<KeranjangItem> Keranjang { get; } = [];

    public event Action<TransaksiSuksesInfo>? TransaksiSukses;

    [ObservableProperty] private string _kataCari = "";
    [ObservableProperty] private BarangKasirDto? _hasilTerpilih;
    [ObservableProperty] private int _inputQty = 1;
    [ObservableProperty] private string _uangBayar = "";
    [ObservableProperty] private string _pesan = "";
    [ObservableProperty] private bool _pesanError;
    [ObservableProperty] private bool _pesanSukses;

    public long Total => Keranjang.Sum(k => k.Subtotal);
    public string TotalTampil => Uang.Format(Total);
    public long Kembalian => Uang.TryParse(UangBayar, out var bayar) ? Math.Max(0, bayar - Total) : 0;
    public string KembalianTampil => Uang.Format(Kembalian);
    public string NomorSukses { get; private set; } = "";

    partial void OnUangBayarChanged(string value)
    {
        OnPropertyChanged(nameof(Kembalian));
        OnPropertyChanged(nameof(KembalianTampil));
    }

    public async Task MuatAsync() => await Cari();

    [RelayCommand]
    private async Task Cari()
    {
        var hasil = await barang.CariUntukKasirAsync(KataCari);
        HasilCari.Clear();
        foreach (var b in hasil.Value ?? []) HasilCari.Add(b);
        // Auto-add dihapus: kasir harus klik tombol "Tambah" manual
    }

    [RelayCommand]
    private void TambahKeKeranjang()
    {
        if (HasilTerpilih is null)
        {
            SetPesan("Pilih barang dari daftar.", true);
            return;
        }
        Tambah(HasilTerpilih);
    }

    public void Tambah(BarangKasirDto dto)
    {
        if (!Uang.TryParse(InputQty.ToString(), out var qty) || qty <= 0)
        {
            SetPesan("Qty tidak valid.", true);
            return;
        }

        var ada = Keranjang.FirstOrDefault(k => k.BarangId == dto.Id);
        if (ada is not null)
        {
            ada.Qty += (int)qty;
        }
        else
        {
            var item = new KeranjangItem(dto) { Qty = (int)qty };
            item.Berubah += PemicuTotal;
            Keranjang.Add(item);
        }

        KataCari = "";
        HasilCari.Clear();
        InputQty = 1;
        SetPesan("", false);
        PemicuTotal();
    }

    [RelayCommand]
    private void HapusItem(KeranjangItem? item)
    {
        if (item is null) return;
        Keranjang.Remove(item);
        PemicuTotal();
    }

    [RelayCommand]
    private async Task Bayar()
    {
        if (Keranjang.Count == 0)
        {
            SetPesan("Keranjang masih kosong.", true);
            return;
        }

        if (!Uang.TryParse(UangBayar, out var bayar))
        {
            SetPesan("Uang bayar tidak valid.", true);
            return;
        }

        if (bayar < Total)
        {
            SetPesan($"Uang bayar kurang dari total ({Uang.Format(Total)}).", true);
            return;
        }

        var total = Total;
        var items = Keranjang.Select(k => new PenjualanItemInput(k.BarangId, k.Qty)).ToList();

        try
        {
            var hasil = await penjualan.BuatPenjualanAsync(new PenjualanInput(items, bayar));
            if (!hasil.Success)
            {
                SetPesan(hasil.Error!, true);
                return;
            }

            NomorSukses = hasil.Value!.NoTransaksi;
            SetPesan($"Transaksi {hasil.Value.NoTransaksi} berhasil. Kembalian {Uang.Format(hasil.Value.Kembalian)}.", false);
            PesanSukses = true;

            TransaksiSukses?.Invoke(new TransaksiSuksesInfo(hasil.Value.NoTransaksi, total, bayar, hasil.Value.Kembalian));

            Keranjang.Clear();
            UangBayar = "";
            PemicuTotal();
        }
        catch (AppException ex)
        {
            SetPesan(ex.Message, true);
        }
    }

    [RelayCommand]
    private void Kosongkan()
    {
        Keranjang.Clear();
        UangBayar = "";
        SetPesan("", false);
        PemicuTotal();
    }

    [RelayCommand]
    private async Task UangPas()
    {
        UangBayar = Total.ToString();
        await Task.CompletedTask;
    }

    private void PemicuTotal()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalTampil));
        OnPropertyChanged(nameof(Kembalian));
        OnPropertyChanged(nameof(KembalianTampil));
    }

    private void SetPesan(string pesan, bool error)
    {
        Pesan = pesan;
        PesanError = error;
        if (error) PesanSukses = false;
    }
}
