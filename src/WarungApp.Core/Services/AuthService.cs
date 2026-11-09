using System.IO;
using WarungApp.Core.Common;
using WarungApp.Core.Entities;
using WarungApp.Core.Enums;
using WarungApp.Core.Interfaces;

namespace WarungApp.Core.Services;

public class AuthService(IUnitOfWork uow, ICurrentUser current, IAuditService? audit = null) : IAuthService
{
    private static readonly Dictionary<string, (int Gagal, DateTime Sampai)> GagalLogin = [];

    public bool NeedsSetup() => !uow.Users.Any();

    public async Task<Result<AuthResult>> SetupOwnerAsync(string username, string namaLengkap, string password)
    {
        if (uow.Users.Any())
            return Result<AuthResult>.Fail("Akun Pemilik sudah ada.");

        var error = ValidasiAkun(username, namaLengkap, password);
        if (error is not null) return Result<AuthResult>.Fail(error);

        var user = BuatUser(username, namaLengkap, password, Role.Pemilik);
        uow.Add(user);
        await uow.SaveChangesAsync();
        await CatatAsync("Setup pemilik", $"Akun '{user.Username}' dibuat.");
        return Result<AuthResult>.Ok(ToResult(user));
    }

    public async Task<Result<AuthResult>> LoginAsync(string username, string password, Role requestedRole = Role.Kasir)
    {
        var key = username.Trim().ToLowerInvariant();
        if (GagalLogin.TryGetValue(key, out var s) && s.Sampai > DateTime.UtcNow)
            return Result<AuthResult>.Fail("Terlalu banyak percobaan. Coba lagi dalam 30 detik.");

        var user = uow.Users.FirstOrDefault(u => u.Username.ToLower() == key && u.Aktif);
        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            var gagal = GagalLogin.TryGetValue(key, out var prev) ? prev.Gagal + 1 : 1;
            GagalLogin[key] = (gagal, gagal >= 5 ? DateTime.UtcNow.AddSeconds(30) : DateTime.MinValue);
            return Result<AuthResult>.Fail("Username atau password salah.");
        }

        if (user.Role != requestedRole)
            return Result<AuthResult>.Fail($"User ini bukan {requestedRole}.");

        GagalLogin.Remove(key);
        current.Set(user);
        await CatatAsync("Login", null);
        return Result<AuthResult>.Ok(ToResult(user));
    }

    public async Task<Result<int>> TambahKasirAsync(string username, string namaLengkap, string password)
    {
        Guard.RequireOwner(current);
        var error = ValidasiAkun(username, namaLengkap, password);
        if (error is not null) return Result<int>.Fail(error);
        if (uow.Users.Any(u => u.Username.ToLower() == username.Trim().ToLower()))
            return Result<int>.Fail("Username sudah dipakai.");

        var user = BuatUser(username, namaLengkap, password, Role.Kasir);
        uow.Add(user);
        await uow.SaveChangesAsync();
        await CatatAsync("Tambah kasir", $"Akun '{user.Username}' dibuat.");
        return Result<int>.Ok(user.Id);
    }

    public async Task<Result> ResetPasswordAsync(int userId, string passwordBaru)
    {
        Guard.RequireOwner(current);
        if (passwordBaru.Length < 6) return Result.Fail("Password minimal 6 karakter.");
        var user = uow.Users.FirstOrDefault(u => u.Id == userId);
        if (user is null) return Result.Fail("User tidak ditemukan.");
        user.PasswordHash = PasswordHasher.Hash(passwordBaru);
        await uow.SaveChangesAsync();
        await CatatAsync("Reset password", $"Akun '{user.Username}'.");
        return Result.Ok();
    }

    public async Task<Result> NonaktifkanAsync(int userId)
    {
        Guard.RequireOwner(current);
        if (userId == current.User!.Id) return Result.Fail("Tidak bisa menonaktifkan akun sendiri.");
        var user = uow.Users.FirstOrDefault(u => u.Id == userId);
        if (user is null) return Result.Fail("User tidak ditemukan.");
        user.Aktif = false;
        await uow.SaveChangesAsync();
        await CatatAsync("Nonaktifkan user", $"Akun '{user.Username}'.");
        return Result.Ok();
    }

    public Task<Result<List<User>>> DaftarUserAsync()
    {
        Guard.RequireOwner(current);
        return Task.FromResult(Result<List<User>>.Ok(uow.Users.ToList()));
    }

    private async Task CatatAsync(string aksi, string? detail)
    {
        if (audit is null) return;
        try { await audit.CatatAsync(aksi, detail); } catch { /* logging tidak boleh menggagalkan alur utama */ }
    }

    private static User BuatUser(string username, string namaLengkap, string password, Role role) => new()
    {
        Username = username.Trim(),
        NamaLengkap = namaLengkap.Trim(),
        PasswordHash = PasswordHasher.Hash(password),
        Role = role,
        Aktif = true,
        DibuatPada = DateTime.UtcNow
    };

    private static AuthResult ToResult(User user) => new() { UserId = user.Id, NamaLengkap = user.NamaLengkap, Role = user.Role };

    private static string? ValidasiAkun(string username, string namaLengkap, string password)
    {
        if (string.IsNullOrWhiteSpace(username)) return "Username wajib diisi.";
        if (username.Contains(' ')) return "Username tidak boleh mengandung spasi.";
        if (string.IsNullOrWhiteSpace(namaLengkap)) return "Nama lengkap wajib diisi.";
        if (password.Length < 6) return "Password minimal 6 karakter.";
        return null;
    }
}




