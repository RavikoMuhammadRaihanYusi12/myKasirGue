using WarungApp.Core.Common;
using WarungApp.Core.Entities;

namespace WarungApp.Core.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
    IQueryable<Barang> Barang { get; }
    IQueryable<Kategori> Kategori { get; }
    IQueryable<User> Users { get; }
    IQueryable<Penjualan> Penjualan { get; }
    IQueryable<PenjualanItem> PenjualanItem { get; }
    IQueryable<StokMasuk> StokMasuk { get; }
    IQueryable<MutasiStok> MutasiStok { get; }
    IQueryable<Penyesuaian> Penyesuaian { get; }
    IQueryable<Pengaturan> Pengaturan { get; }
    IQueryable<LogAktivitas> LogAktivitas { get; }

    void Add<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    void Reload();
    Task ExecuteInTransactionAsync(Func<Task> action);
}
