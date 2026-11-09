using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;

namespace WarungApp.Data;

/// <summary>
/// Backup memakai SQLite Online Backup API (bukan copy file mentah saat DB terbuka, rawan korup di WAL).
/// ponytail: retensi = N file terbaru per pola nama; ganti ke pengelolaan folder bila perlu.
/// </summary>
public class BackupService(
    IDbContextFactory<AppDbContext> dbFactory,
    ISettingsService settings) : IBackupService
{
    private const string PolaBackup = "warung-*.db";

    public async Task<Result<string>> BackupAsync(string? tujuan = null)
    {
        var folder = tujuan ?? await FolderBackupAktifAsync();
        Directory.CreateDirectory(folder);

        var nama = $"warung-{DateTime.Now:yyyyMMdd-HHmmss}.db";
        var target = Path.Combine(folder, nama);

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            await using var sumber = new SqliteConnection(db.Database.GetConnectionString());
            await using var salinan = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = target }.ToString());

            await sumber.OpenAsync();
            await salinan.OpenAsync();
            sumber.BackupDatabase(salinan);

            if (!await CekIntegritasAsync(salinan))
                return Result<string>.Fail("File backup gagal diperiksa (integrity_check). Backup dibatalkan.");

            // Tutup bersih + checkpoint agar tidak meninggalkan file -wal/-shm di samping backup.
            using (var bersih = salinan.CreateCommand())
            {
                bersih.CommandText = "PRAGMA wal_checkpoint(TRUNCATE); PRAGMA journal_mode=DELETE;";
                await bersih.ExecuteNonQueryAsync();
            }
            SqliteConnection.ClearAllPools();

            await RotasiAsync(folder);
            return Result<string>.Ok(target);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Backup gagal: {ex.Message}");
        }
    }

    public async Task<Result> RestoreAsync(string file)
    {
        if (!File.Exists(file)) return Result.Fail("File backup tidak ditemukan.");

        try
        {
            await using (var uji = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file }.ToString()))
            {
                await uji.OpenAsync();
                if (!await CekIntegritasAsync(uji)) return Result.Fail("File backup rusak (integrity_check gagal).");
                await using var cek = uji.CreateCommand();
                cek.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Users'";
                if (Convert.ToInt64(await cek.ExecuteScalarAsync()) == 0)
                    return Result.Fail("File bukan backup WarungApp (tabel Users tidak ada).");
            }

            // Backup pengaman dulu, baru timpa.
            var pengaman = await BackupAsync();
            if (!pengaman.Success) return Result.Fail("Gagal membuat backup pengaman sebelum restore.");

            await using var db = await dbFactory.CreateDbContextAsync();
            await db.Database.CloseConnectionAsync();

            await using (var sumber = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file }.ToString()))
            await using (var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = DbPath.DbFile }.ToString()))
            {
                await sumber.OpenAsync();
                await target.OpenAsync();
                sumber.BackupDatabase(target);
            }

            await using var setelah = await dbFactory.CreateDbContextAsync();
            await setelah.Database.MigrateAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Restore gagal: {ex.Message}");
        }
    }

    public async Task<List<string>> DaftarBackupAsync()
    {
        var folder = await FolderBackupAktifAsync();
        if (!Directory.Exists(folder)) return [];
        return Directory.GetFiles(folder, PolaBackup).OrderByDescending(f => f).ToList();
    }

    public async Task<string> FolderBackupAktifAsync()
    {
        var set = await settings.AmbilAsync();
        return string.IsNullOrWhiteSpace(set.FolderBackup)
            ? Path.Combine(DbPath.BaseFolder, "backup")
            : set.FolderBackup;
    }

    private async Task RotasiAsync(string folder)
    {
        var set = await settings.AmbilAsync();
        var semua = Directory.GetFiles(folder, PolaBackup).OrderByDescending(f => f).ToList();
        foreach (var lama in semua.Skip(set.RetensiBackup))
        {
            try { File.Delete(lama); } catch { /* file terkunci: lewati */ }
        }
    }

    private static async Task<bool> CekIntegritasAsync(SqliteConnection conn)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA integrity_check";
        return string.Equals(await cmd.ExecuteScalarAsync() as string, "ok", StringComparison.OrdinalIgnoreCase);
    }
}
