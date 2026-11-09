namespace WarungApp.Core.Entities;

public class Penyesuaian
{
    public int Id { get; set; }
    public DateTime Tanggal { get; set; } = DateTime.Now;
    public int BarangId { get; set; }
    public int StokSistem { get; set; }
    public int StokFisik { get; set; }
    public int Selisih { get; set; }
    public string Alasan { get; set; } = "";
    public int UserId { get; set; }
}
