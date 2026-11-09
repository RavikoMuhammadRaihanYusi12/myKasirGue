namespace WarungApp.Core.Entities;

public class StokMasuk
{
    public int Id { get; set; }
    public DateTime Tanggal { get; set; } = DateTime.Now;
    public string? Supplier { get; set; }
    public string? Catatan { get; set; }
    public int UserId { get; set; }

    public List<StokMasukItem> Items { get; set; } = [];
}

public class StokMasukItem
{
    public int Id { get; set; }
    public int StokMasukId { get; set; }
    public int BarangId { get; set; }
    public int Qty { get; set; }
    public long HargaBeli { get; set; }
}
