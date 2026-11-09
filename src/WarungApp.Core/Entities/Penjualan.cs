using WarungApp.Core.Enums;

namespace WarungApp.Core.Entities;

public class Penjualan
{
    public int Id { get; set; }
    public string NoTransaksi { get; set; } = "";
    public DateTime Tanggal { get; set; } = DateTime.Now;
    public int UserId { get; set; }
    public long Total { get; set; }
    public long Bayar { get; set; }
    public long Kembalian { get; set; }
    public StatusPenjualan Status { get; set; } = StatusPenjualan.Selesai;
    public string? AlasanVoid { get; set; }
    public int? VoidOlehUserId { get; set; }

    public List<PenjualanItem> Items { get; set; } = [];
}

public class PenjualanItem
{
    public int Id { get; set; }
    public int PenjualanId { get; set; }
    public int BarangId { get; set; }
    public int Qty { get; set; }
    public long HargaJual { get; set; }
    public long HargaBeli { get; set; }
    public long Subtotal { get; set; }
}
