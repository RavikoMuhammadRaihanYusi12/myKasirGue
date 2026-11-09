using Microsoft.EntityFrameworkCore;
using WarungApp.Core.Entities;

namespace WarungApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Kategori> Kategori => Set<Kategori>();
    public DbSet<Barang> Barang => Set<Barang>();
    public DbSet<Penjualan> Penjualan => Set<Penjualan>();
    public DbSet<PenjualanItem> PenjualanItem => Set<PenjualanItem>();
    public DbSet<StokMasuk> StokMasuk => Set<StokMasuk>();
    public DbSet<StokMasukItem> StokMasukItem => Set<StokMasukItem>();
    public DbSet<Penyesuaian> Penyesuaian => Set<Penyesuaian>();
    public DbSet<MutasiStok> MutasiStok => Set<MutasiStok>();
    public DbSet<Pengaturan> Pengaturan => Set<Pengaturan>();
    public DbSet<LogAktivitas> LogAktivitas => Set<LogAktivitas>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        Configurations.ModelConfig.Configure(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
