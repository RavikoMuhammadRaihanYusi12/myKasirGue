using WarungApp.Core.Common;
using WarungApp.Core.Entities;
using WarungApp.Core.Enums;
using WarungApp.Core.Interfaces;

namespace WarungApp.Core.Services;

public class StokService(IUnitOfWork uow, ICurrentUser current, IAuditService? audit = null) : IStokService
{
    public async Task<Result<int>> SimpanStokMasukAsync(StokMasukInput input)
    {
        Guard.RequireOwner(current);
        if (input.Items.Count == 0) return Result<int>.Fail("Tambahkan minimal satu barang.");
        if (input.Items.Any(i => i.Qty <= 0)) return Result<int>.Fail("Qty harus lebih dari 0.");
        if (input.Items.Any(i => i.HargaBeli < 0)) return Result<int>.Fail("Harga beli tidak boleh negatif.");

        var stokMasuk = new StokMasuk
        {
            Tanggal = input.Tanggal,
            Supplier = string.IsNullOrWhiteSpace(input.Supplier) ? null : input.Supplier.Trim(),
            Catatan = string.IsNullOrWhiteSpace(input.Catatan) ? null : input.Catatan.Trim(),
            UserId = current.User!.Id
        };

        await uow.ExecuteInTransactionAsync(async () =>
        {
            uow.Add(stokMasuk);
            await uow.SaveChangesAsync();

            foreach (var item in input.Items)
            {
                var barang = uow.Barang.FirstOrDefault(b => b.Id == item.BarangId && b.Aktif)
                    ?? throw new AppException($"Barang id {item.BarangId} tidak ditemukan atau nonaktif.");

                var sebelum = barang.Stok;
                barang.Stok += item.Qty;
                if (input.UpdateHargaBeli) barang.HargaBeli = item.HargaBeli;

                stokMasuk.Items.Add(new StokMasukItem
                {
                    StokMasukId = stokMasuk.Id,
                    BarangId = barang.Id,
                    Qty = item.Qty,
                    HargaBeli = item.HargaBeli
                });

                uow.Add(new MutasiStok
                {
                    BarangId = barang.Id,
                    Jenis = JenisMutasi.StokMasuk,
                    Qty = item.Qty,
                    StokSebelum = sebelum,
                    StokSesudah = barang.Stok,
                    ReferensiId = stokMasuk.Id,
                    Waktu = DateTime.Now,
                    UserId = current.User!.Id
                });
            }
        });

        await CatatAsync("Stok masuk",
            $"{input.Items.Count} item, supplier {(string.IsNullOrWhiteSpace(input.Supplier) ? "-" : input.Supplier)}.");
        return Result<int>.Ok(stokMasuk.Id);
    }
    public Task<Result<List<StokMasukDto>>> DaftarStokMasukAsync(DateTime? dari, DateTime? sampai)
    {
        Guard.RequireOwner(current);
        var q = uow.StokMasuk.AsQueryable();
        if (dari is DateTime d) q = q.Where(s => s.Tanggal >= d.Date);
        if (sampai is DateTime s2) q = q.Where(s => s.Tanggal < s2.Date.AddDays(1));

        var data = q.OrderByDescending(s => s.Tanggal).Take(200).ToList();
        var userIds = data.Select(s => s.UserId).Distinct().ToList();
        var users = uow.Users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.NamaLengkap);
        var barangMap = uow.Barang.ToDictionary(b => b.Id, b => b.Nama);

        var list = data.Select(s => new StokMasukDto(
            s.Id, s.Tanggal, s.Supplier, s.Catatan,
            users.GetValueOrDefault(s.UserId, "-"),
            s.Items.Sum(i => i.Qty * i.HargaBeli),
            s.Items.Select(i => new StokMasukItemDto(i.BarangId, barangMap.GetValueOrDefault(i.BarangId, "-"),
                i.Qty, i.HargaBeli, i.Qty * i.HargaBeli)).ToList()
        )).ToList();

        return Task.FromResult(Result<List<StokMasukDto>>.Ok(list));
    }

    public async Task<Result<int>> SimpanOpnameAsync(OpnameInput input)
    {
        Guard.RequireOwner(current);
        if (input.StokFisik < 0) return Result<int>.Fail("Stok fisik tidak boleh negatif.");
        if (string.IsNullOrWhiteSpace(input.Alasan)) return Result<int>.Fail("Alasan wajib dipilih.");

        var barang = uow.Barang.FirstOrDefault(b => b.Id == input.BarangId);
        if (barang is null) return Result<int>.Fail("Barang tidak ditemukan.");

        var penyesuaian = new Penyesuaian
        {
            Tanggal = DateTime.Now,
            BarangId = barang.Id,
            StokSistem = barang.Stok,
            StokFisik = input.StokFisik,
            Selisih = input.StokFisik - barang.Stok,
            Alasan = input.Alasan.Trim(),
            UserId = current.User!.Id
        };

        await uow.ExecuteInTransactionAsync(() =>
        {
            var sebelum = barang.Stok;
            barang.Stok = input.StokFisik;
            uow.Add(penyesuaian);
            uow.Add(new MutasiStok
            {
                BarangId = barang.Id,
                Jenis = JenisMutasi.Opname,
                Qty = input.StokFisik - sebelum,
                StokSebelum = sebelum,
                StokSesudah = input.StokFisik,
                ReferensiId = penyesuaian.Id,
                Waktu = DateTime.Now,
                UserId = current.User!.Id
            });
            return Task.CompletedTask;
        });

        await CatatAsync("Opname",
            $"{barang.Nama}: sistem {penyesuaian.StokSistem}, fisik {penyesuaian.StokFisik}, alasan {penyesuaian.Alasan}.");
        return Result<int>.Ok(penyesuaian.Id);
    }
    public Task<Result<List<PenyesuaianDto>>> DaftarOpnameAsync(DateTime? dari, DateTime? sampai)
    {
        Guard.RequireOwner(current);
        var q = uow.Penyesuaian.AsQueryable();
        if (dari is DateTime d) q = q.Where(p => p.Tanggal >= d.Date);
        if (sampai is DateTime s2) q = q.Where(p => p.Tanggal < s2.Date.AddDays(1));

        var data = q.OrderByDescending(p => p.Tanggal).Take(200).ToList();
        var barangMap = uow.Barang.ToDictionary(b => b.Id, b => b.Nama);
        var users = uow.Users.ToDictionary(u => u.Id, u => u.NamaLengkap);

        var list = data.Select(p => new PenyesuaianDto(p.Id, p.Tanggal, barangMap.GetValueOrDefault(p.BarangId, "-"),
            p.StokSistem, p.StokFisik, p.Selisih, p.Alasan, users.GetValueOrDefault(p.UserId, "-"))).ToList();

        return Task.FromResult(Result<List<PenyesuaianDto>>.Ok(list));
    }

    public Task<Result<List<MutasiDto>>> KartuStokAsync(int barangId)
    {
        Guard.RequireOwner(current);
        var data = uow.MutasiStok.Where(m => m.BarangId == barangId)
            .OrderByDescending(m => m.Waktu).Take(300).ToList();
        var namaBarang = uow.Barang.Where(b => b.Id == barangId).Select(b => b.Nama).FirstOrDefault() ?? "-";
        var users = uow.Users.ToDictionary(u => u.Id, u => u.NamaLengkap);

        var list = data.Select(m => new MutasiDto(m.Id, m.Waktu, namaBarang, m.Jenis.ToString(),
            m.Qty, m.StokSebelum, m.StokSesudah, users.GetValueOrDefault(m.UserId, "-"))).ToList();

        return Task.FromResult(Result<List<MutasiDto>>.Ok(list));
    }

    private async Task CatatAsync(string aksi, string? detail)
    {
        if (audit is null) return;
        try { await audit.CatatAsync(aksi, detail); } catch { /* log tidak menggagalkan transaksi */ }
    }
}
