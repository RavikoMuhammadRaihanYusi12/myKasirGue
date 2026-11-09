using WarungApp.Core.Common;
using WarungApp.Core.Enums;

namespace WarungApp.Core.Interfaces;

public class AuthResult
{
    public int UserId { get; init; }
    public string NamaLengkap { get; init; } = "";
    public Role Role { get; init; }
}

public interface IAuthService
{
    bool NeedsSetup();
    Task<Result<AuthResult>> SetupOwnerAsync(string username, string namaLengkap, string password);
    Task<Result<AuthResult>> LoginAsync(string username, string password, Role requestedRole = Role.Kasir);
    Task<Result<int>> TambahKasirAsync(string username, string namaLengkap, string password);
    Task<Result> ResetPasswordAsync(int userId, string passwordBaru);
    Task<Result> NonaktifkanAsync(int userId);
    Task<Result<List<Entities.User>>> DaftarUserAsync();
}
