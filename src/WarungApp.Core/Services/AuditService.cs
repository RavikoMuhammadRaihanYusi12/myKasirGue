using WarungApp.Core.Common;
using WarungApp.Core.Entities;
using WarungApp.Core.Enums;
using WarungApp.Core.Interfaces;

namespace WarungApp.Core.Services;

public class AuditService(IUnitOfWork uow, ICurrentUser current) : IAuditService
{
    private static readonly SemaphoreSlim Kunci = new(1, 1);

    public async Task CatatAsync(string aksi, string? detail = null)
    {
        // ponytail: serialisasi penulisan log; cukup untuk aplikasi desktop satu pengguna.
        await Kunci.WaitAsync();
        try
        {
            uow.Add(new LogAktivitas
            {
                Waktu = DateTime.Now,
                UserId = current.User?.Id,
                Aksi = aksi,
                Detail = detail
            });
            await uow.SaveChangesAsync();
        }
        finally
        {
            Kunci.Release();
        }
    }

    public Task<Result<List<LogDto>>> DaftarAsync(DateTime? dari, DateTime? sampai)
    {
        Guard.RequireOwner(current);

        var q = uow.LogAktivitas.AsQueryable();
        if (dari is DateTime d) q = q.Where(l => l.Waktu >= d.Date);
        if (sampai is DateTime s) q = q.Where(l => l.Waktu < s.Date.AddDays(1));

        var data = q.OrderByDescending(l => l.Id).Take(500).ToList();
        var users = uow.Users.ToDictionary(u => u.Id, u => u.NamaLengkap);

        var list = data.Select(l => new LogDto(l.Id, l.Waktu,
            l.UserId is int uid ? users.GetValueOrDefault(uid, $"#{uid}") : "sistem",
            l.Aksi, l.Detail)).ToList();

        return Task.FromResult(Result<List<LogDto>>.Ok(list));
    }
}

public class UserService(IAuthService auth) : IUserService
{
    public async Task<Result<List<UserDto>>> DaftarAsync()
    {
        var hasil = await auth.DaftarUserAsync();
        if (!hasil.Success) return Result<List<UserDto>>.Fail(hasil.Error!);

        var list = hasil.Value!.Select(u => new UserDto(u.Id, u.Username, u.NamaLengkap, u.Role.ToString(),
            u.Aktif, u.DibuatPada.ToLocalTime())).ToList();
        return Result<List<UserDto>>.Ok(list);
    }

    public async Task<Result<int>> TambahKasirAsync(string username, string namaLengkap, string password)
    {
        var hasil = await auth.TambahKasirAsync(username, namaLengkap, password);
        return hasil.Success ? Result<int>.Ok(hasil.Value) : Result<int>.Fail(hasil.Error!);
    }

    public Task<Result> ResetPasswordAsync(int userId, string passwordBaru) =>
        auth.ResetPasswordAsync(userId, passwordBaru);

    public async Task<Result> SetAktifAsync(int userId, bool aktif)
    {
        if (!aktif) return await auth.NonaktifkanAsync(userId);
        return Result.Fail("Mengaktifkan kembali user belum didukung.");
    }
}
