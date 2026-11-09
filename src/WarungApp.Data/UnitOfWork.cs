using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WarungApp.Core.Entities;
using WarungApp.Core.Interfaces;

namespace WarungApp.Data;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public IQueryable<Barang> Barang => db.Barang;
    public IQueryable<Kategori> Kategori => db.Kategori;
    public IQueryable<User> Users => db.Users;
    public IQueryable<Penjualan> Penjualan => db.Penjualan;
    public IQueryable<PenjualanItem> PenjualanItem => db.PenjualanItem;
    public IQueryable<StokMasuk> StokMasuk => db.StokMasuk;
    public IQueryable<MutasiStok> MutasiStok => db.MutasiStok;
    public IQueryable<Penyesuaian> Penyesuaian => db.Penyesuaian;
    public IQueryable<Pengaturan> Pengaturan => db.Pengaturan;
    public IQueryable<LogAktivitas> LogAktivitas => db.LogAktivitas;

    public void Add<T>(T entity) where T : class => db.Set<T>().Add(entity);
    public void Remove<T>(T entity) where T : class => db.Set<T>().Remove(entity);

    /// <summary>Buang semua state yang dilacak setelah rollback, agar percobaan berikutnya bersih.</summary>
    public void Reload() => db.ChangeTracker.Clear();
    public Task SaveChangesAsync() => db.SaveChangesAsync();

    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await action();
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}

public static class DbPath
{
    /// <summary>Bisa diarahkan ke folder lain (uji / restore).</summary>
    public static string? OverrideFolder { get; set; }

    public static string BaseFolder => OverrideFolder
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WarungApp");

    public static string DbFile => Path.Combine(BaseFolder, "warung.db");
    public static string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = DbFile,
        ForeignKeys = true
    }.ToString();
}
