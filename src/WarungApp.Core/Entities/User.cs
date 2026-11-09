using WarungApp.Core.Enums;

namespace WarungApp.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string NamaLengkap { get; set; } = "";
    public Role Role { get; set; }
    public bool Aktif { get; set; } = true;
    public DateTime DibuatPada { get; set; } = DateTime.UtcNow;
}
