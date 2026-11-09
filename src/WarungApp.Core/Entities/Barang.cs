namespace WarungApp.Core.Entities;

public class Barang
{
    public int Id { get; set; }
    public string? Kode { get; set; }
    public string Nama { get; set; } = "";
    public int KategoriId { get; set; }
    public string Satuan { get; set; } = "pcs";
    public long HargaBeli { get; set; }
    public long HargaJual { get; set; }
    public int Stok { get; set; }
    public int StokMinimum { get; set; }
    public bool Aktif { get; set; } = true;
    public int RowVersion { get; set; }

    public Kategori? Kategori { get; set; }
}
