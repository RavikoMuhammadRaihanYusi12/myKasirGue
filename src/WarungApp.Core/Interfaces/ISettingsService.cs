using WarungApp.Core.Common;

namespace WarungApp.Core.Interfaces;

public record PengaturanDto(string NamaToko, string Alamat, string Telepon, bool BolehStokMinus,
    string FolderBackup, int RetensiBackup);

public record LogDto(long Id, DateTime Waktu, string User, string Aksi, string? Detail);

public interface ISettingsService
{
    Task<PengaturanDto> AmbilAsync();
    Task<Result> SimpanAsync(PengaturanDto dto);
}

public interface IAuditService
{
    Task CatatAsync(string aksi, string? detail = null);
    Task<Result<List<LogDto>>> DaftarAsync(DateTime? dari, DateTime? sampai);
}

public interface IBackupService
{
    Task<Result<string>> BackupAsync(string? tujuan = null);
    Task<Result> RestoreAsync(string file);
    Task<List<string>> DaftarBackupAsync();
    Task<string> FolderBackupAktifAsync();
}

public interface IUserService
{
    Task<Result<List<UserDto>>> DaftarAsync();
    Task<Result<int>> TambahKasirAsync(string username, string namaLengkap, string password);
    Task<Result> ResetPasswordAsync(int userId, string passwordBaru);
    Task<Result> SetAktifAsync(int userId, bool aktif);
}

public record UserDto(int Id, string Username, string NamaLengkap, string Role, bool Aktif, DateTime DibuatPada);
