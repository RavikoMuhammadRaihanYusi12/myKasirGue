using Microsoft.EntityFrameworkCore;
using WarungApp.Core.Entities;
using WarungApp.Core.Enums;

namespace WarungApp.Data.Configurations;

public static class ModelConfig
{
    public static void Configure(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).UseCollation("NOCASE");
            e.Property(x => x.Role).HasConversion<int>();
        });

        b.Entity<Kategori>(e => e.HasIndex(x => x.Nama).IsUnique());

        b.Entity<Barang>(e =>
        {
            e.HasIndex(x => x.Nama);
            e.HasIndex(x => x.Kode).IsUnique().HasFilter("\"Kode\" IS NOT NULL");
            e.HasOne(x => x.Kategori).WithMany().HasForeignKey(x => x.KategoriId);
        });

        b.Entity<Penjualan>(e =>
        {
            e.HasIndex(x => x.NoTransaksi).IsUnique();
            e.HasIndex(x => x.Tanggal);
            e.Property(x => x.Status).HasConversion<int>();
        });

        b.Entity<PenjualanItem>(e =>
        {
            e.HasOne<Penjualan>().WithMany(p => p.Items).HasForeignKey(x => x.PenjualanId);
            e.HasOne<Barang>().WithMany().HasForeignKey(x => x.BarangId);
            e.HasIndex(x => x.PenjualanId);
            e.HasIndex(x => x.BarangId);
        });

        b.Entity<StokMasuk>(e =>
            e.HasMany(s => s.Items).WithOne().HasForeignKey(x => x.StokMasukId));
        b.Entity<StokMasukItem>(e =>
        {
            e.HasOne<Barang>().WithMany().HasForeignKey(x => x.BarangId);
        });

        b.Entity<Penyesuaian>(e => e.HasOne<Barang>().WithMany().HasForeignKey(x => x.BarangId));

        b.Entity<MutasiStok>(e =>
        {
            e.HasIndex(x => new { x.BarangId, x.Waktu });
            e.Property(x => x.Jenis).HasConversion<int>();
            e.HasOne<Barang>().WithMany().HasForeignKey(x => x.BarangId);
        });

        b.Entity<Pengaturan>(e => e.HasKey(x => x.Kunci));
    }
}
