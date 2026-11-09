using WarungApp.Core.Enums;

namespace WarungApp.Core.Entities;

public class MutasiStok
{
    public int Id { get; set; }
    public int BarangId { get; set; }
    public JenisMutasi Jenis { get; set; }
    public int Qty { get; set; }
    public int StokSebelum { get; set; }
    public int StokSesudah { get; set; }
    public int? ReferensiId { get; set; }
    public DateTime Waktu { get; set; } = DateTime.Now;
    public int UserId { get; set; }
}
