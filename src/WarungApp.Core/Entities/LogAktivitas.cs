namespace WarungApp.Core.Entities;

public class LogAktivitas
{
    public long Id { get; set; }
    public DateTime Waktu { get; set; } = DateTime.Now;
    public int? UserId { get; set; }
    public string Aksi { get; set; } = "";
    public string? Detail { get; set; }
}
