using WarungApp.Core.Entities;
using WarungApp.Core.Enums;

namespace WarungApp.Core.Common;

public interface ICurrentUser
{
    User? User { get; }
    bool IsLoggedIn { get; }
    void Set(User user);
    void Clear();
}

public static class Guard
{
    public static void RequireOwner(ICurrentUser current)
    {
        if (current.User is null || current.User.Role != Role.Pemilik)
            throw new ForbiddenException("Akses ditolak: fitur ini hanya untuk Pemilik.");
    }

    /// <summary>Versi non-exception untuk UI: memberi pesan error, bukan menabrak aplikasi.</summary>
    public static bool BolehPemilik(ICurrentUser current, out string? error)
    {
        error = current.User?.Role == Role.Pemilik
            ? null
            : "Akses ditolak: fitur ini hanya untuk Pemilik.";
        return error is null;
    }
}

public class AppException(string message) : Exception(message);

public class ForbiddenException(string message) : Exception(message);

