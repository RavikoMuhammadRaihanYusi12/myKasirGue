using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public partial class RiwayatViewModel(IPenjualanService penjualan, ICurrentUser current) : ObservableObject
{
    public ObservableCollection<PenjualanDto> Daftar { get; } = [];
    public ObservableCollection<PenjualanItemDto> ItemTerpilih { get; } = [];

    public bool IsPemilik => current.User?.Role == Core.Enums.Role.Pemilik;

    [ObservableProperty] private DateTime? _dari = DateTime.Today;
    [ObservableProperty] private DateTime? _sampai = DateTime.Today;
    [ObservableProperty] private PenjualanDto? _terpilih;
    [ObservableProperty] private string _pesan = "";
    [ObservableProperty] private bool _pesanError;

    public string Ringkasan =>
        $"Jumlah transaksi: {Daftar.Count(p => p.Status == "Selesai")} · " +
        $"Omzet: {Uang.Format(Daftar.Where(p => p.Status == "Selesai").Sum(p => p.Total))}";

    public async Task MuatAsync()
    {
        var hasil = await penjualan.DaftarAsync(Dari, Sampai, false);
        Daftar.Clear();
        foreach (var p in hasil.Value ?? []) Daftar.Add(p);
        OnPropertyChanged(nameof(Ringkasan));
        ItemTerpilih.Clear();
    }

    [RelayCommand]
    private Task Muat() => MuatAsync();

    [RelayCommand]
    private void Pilih(PenjualanDto? dto)
    {
        Terpilih = dto;
        ItemTerpilih.Clear();
        if (dto is null) return;
        foreach (var i in dto.Items) ItemTerpilih.Add(i);
    }

    [RelayCommand]
    private async Task Void()
    {
        if (Terpilih is null)
        {
            SetPesan("Pilih transaksi terlebih dahulu.", true);
            return;
        }

        var alasan = TanyaAlasan();
        if (alasan is null) return;

        var hasil = await penjualan.VoidAsync(Terpilih.Id, alasan);
        SetPesan(hasil.Success ? "Transaksi dibatalkan, stok dikembalikan." : hasil.Error!, !hasil.Success);
        if (hasil.Success) await MuatAsync();
    }

    private static string? TanyaAlasan()
    {
        var dialog = new Views.InputDialog("Alasan pembatalan transaksi (void):");
        return dialog.ShowDialog() == true ? dialog.Jawaban : null;
    }

    private void SetPesan(string pesan, bool error)
    {
        Pesan = pesan;
        PesanError = error;
    }
}
