using WarungApp.Core.Common;
using WarungApp.Core.Entities;
using WarungApp.Core.Enums;
using WarungApp.Core.Interfaces;

namespace WarungApp.Core.Services;

public class PenjualanService(IUnitOfWork uow, ICurrentUser current, IAuditService? audit = null) : IPenjualanService
{
    public async Task<Result<PenjualanBerhasil>> BuatPenjualanAsync(PenjualanInput input)
    {
        if (!current.IsLoggedIn) return Result<PenjualanBerhasil>.Fail("Silakan login terlebih dahulu.");
        if (input.Items.Count == 0) return Result<PenjualanBerhasil>.Fail("Keranjang masih kosong.");
        if (input.Items.Any(i => i.Qty <= 0)) return Result<PenjualanBerhasil>.Fail("Qty harus lebih dari 0.");

        // Gabungkan item duplikat agar stok dicek sekali.
        var diminta = input.Items
            .GroupBy(i => i.BarangId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

        var bolehMinus = BolehStokMinus();
        var penjualan = new Penjualan
        {
            Tanggal = DateTime.Now,
            UserId = current.User!.Id,
            Status = StatusPenjualan.Selesai
        };

        long total = 0;
        long laba = 0;

        uow.Add(penjualan);

        try
        {
            await uow.ExecuteInTransactionAsync(async () =>
            {
            penjualan.NoTransaksi = "PENDING";
            await uow.SaveChangesAsync();
            penjualan.NoTransaksi = NomorBerikutnya(penjualan.Id);
            await uow.SaveChangesAsync();

            foreach (var (barangId, qty) in diminta)
            {
                var barang = uow.Barang.FirstOrDefault(b => b.Id == barangId)
                    ?? throw new AppException("Barang di keranjang sudah tidak tersedia.");

                if (!barang.Aktif) throw new AppException($"Barang {barang.Nama} sudah nonaktif.");
                if (!bolehMinus && barang.Stok < qty)
                    throw new AppException($"Stok {barang.Nama} tidak cukup (tersisa {barang.Stok}).");

                var subtotal = barang.HargaJual * qty;
                total += subtotal;
                laba += (barang.HargaJual - barang.HargaBeli) * qty;

                var sebelum = barang.Stok;
                barang.Stok -= qty;

                uow.Add(new PenjualanItem
                {
                    PenjualanId = penjualan.Id,
                    BarangId = barang.Id,
                    Qty = qty,
                    HargaJual = barang.HargaJual,
                    HargaBeli = barang.HargaBeli,
                    Subtotal = subtotal
                });

                uow.Add(new MutasiStok
                {
                    BarangId = barang.Id,
                    Jenis = JenisMutasi.Penjualan,
                    Qty = -qty,
                    StokSebelum = sebelum,
                    StokSesudah = barang.Stok,
                    ReferensiId = penjualan.Id,
                    Waktu = DateTime.Now,
                    UserId = current.User!.Id
                });
            }

            if (input.Bayar < total)
                throw new AppException($"Uang bayar kurang dari total ({Uang.Format(total)}).");

            penjualan.Total = total;
            penjualan.Bayar = input.Bayar;
            penjualan.Kembalian = input.Bayar - total;
            });
        }
        catch
        {
            // Rollback DB tidak otomatis membatalkan entitas yang masih dilacak; samakan state di memori.
            penjualan.Items.Clear();
            uow.Reload();
            throw;
        }

        await CatatAsync("Transaksi", $"{penjualan.NoTransaksi} total {total}, bayar {input.Bayar}.");
        return Result<PenjualanBerhasil>.Ok(new PenjualanBerhasil(
            penjualan.Id, penjualan.NoTransaksi, total, penjualan.Kembalian, laba));
    }

    public async Task<Result> VoidAsync(int penjualanId, string alasan)
    {
        Guard.RequireOwner(current);
        if (string.IsNullOrWhiteSpace(alasan)) return Result.Fail("Alasan void wajib diisi.");

        var penjualan = uow.Penjualan.FirstOrDefault(p => p.Id == penjualanId);
        if (penjualan is null) return Result.Fail("Transaksi tidak ditemukan.");
        if (penjualan.Status == StatusPenjualan.Void) return Result.Fail("Transaksi sudah dibatalkan.");

        var items = uow.PenjualanItem.Where(i => i.PenjualanId == penjualanId).ToList();

        await uow.ExecuteInTransactionAsync(() =>
        {
            penjualan.Status = StatusPenjualan.Void;
            penjualan.AlasanVoid = alasan.Trim();
            penjualan.VoidOlehUserId = current.User!.Id;

            foreach (var item in items)
            {
                var barang = uow.Barang.FirstOrDefault(b => b.Id == item.BarangId);
                if (barang is null) continue;

                var sebelum = barang.Stok;
                barang.Stok += item.Qty;
                uow.Add(new MutasiStok
                {
                    BarangId = barang.Id,
                    Jenis = JenisMutasi.Void,
                    Qty = item.Qty,
                    StokSebelum = sebelum,
                    StokSesudah = barang.Stok,
                    ReferensiId = penjualan.Id,
                    Waktu = DateTime.Now,
                    UserId = current.User!.Id
                });
            }

            return Task.CompletedTask;
        });

        await CatatAsync("Void transaksi", $"{penjualan.NoTransaksi}: {alasan.Trim()}");
        return Result.Ok();
    }

    public Task<Result<List<PenjualanDto>>> DaftarAsync(DateTime? dari, DateTime? sampai, bool hanyaMilikSendiri)
    {
        if (!current.IsLoggedIn) return Task.FromResult(Result<List<PenjualanDto>>.Fail("Silakan login."));

        var q = uow.Penjualan.AsQueryable();
        var isKasir = current.User!.Role == Role.Kasir;

        if (isKasir)
        {
            // Kasir hanya boleh melihat ringkasan transaksi miliknya hari ini, tanpa laba.
            var awalHari = DateTime.Today;
            q = q.Where(p => p.UserId == current.User.Id && p.Tanggal >= awalHari && p.Tanggal < awalHari.AddDays(1));
        }
        else
        {
            if (dari is DateTime d) q = q.Where(p => p.Tanggal >= d.Date);
            if (sampai is DateTime s) q = q.Where(p => p.Tanggal < s.Date.AddDays(1));
            if (hanyaMilikSendiri) q = q.Where(p => p.UserId == current.User.Id);
        }

        var data = q.OrderByDescending(p => p.Id).Take(200).ToList();
        var names = uow.Users.ToDictionary(u => u.Id, u => u.NamaLengkap);
        var barangMap = uow.Barang.ToDictionary(b => b.Id, b => b.Nama);

        var list = data.Select(p => new PenjualanDto(
            p.Id, p.NoTransaksi, p.Tanggal, names.GetValueOrDefault(p.UserId, "-"),
            p.Total, p.Bayar, p.Kembalian, p.Status.ToString(), p.AlasanVoid,
            p.Items.Select(i => new PenjualanItemDto(i.BarangId, barangMap.GetValueOrDefault(i.BarangId, "-"),
                i.Qty, i.HargaJual, i.HargaBeli, i.Subtotal)).ToList()
        )).ToList();

        return Task.FromResult(Result<List<PenjualanDto>>.Ok(list));
    }

    public Task<Result<PenjualanDto>> DetailAsync(int id)
    {
        if (!current.IsLoggedIn) return Task.FromResult(Result<PenjualanDto>.Fail("Silakan login."));

        var p = uow.Penjualan.FirstOrDefault(x => x.Id == id);
        if (p is null) return Task.FromResult(Result<PenjualanDto>.Fail("Transaksi tidak ditemukan."));
        if (current.User!.Role == Role.Kasir && p.UserId != current.User.Id)
            return Task.FromResult(Result<PenjualanDto>.Fail("Transaksi tidak ditemukan."));

        var namaKasir = uow.Users.Where(u => u.Id == p.UserId).Select(u => u.NamaLengkap).FirstOrDefault() ?? "-";
        var barangMap = uow.Barang.ToDictionary(b => b.Id, b => b.Nama);
        var items = uow.PenjualanItem.Where(i => i.PenjualanId == id).ToList()
            .Select(i => new PenjualanItemDto(i.BarangId, barangMap.GetValueOrDefault(i.BarangId, "-"),
                i.Qty, i.HargaJual, i.HargaBeli, i.Subtotal)).ToList();

        return Task.FromResult(Result<PenjualanDto>.Ok(new PenjualanDto(p.Id, p.NoTransaksi, p.Tanggal, namaKasir,
            p.Total, p.Bayar, p.Kembalian, p.Status.ToString(), p.AlasanVoid, items)));
    }

    private bool BolehStokMinus()
    {
        var nilai = uow.Pengaturan.Where(p => p.Kunci == "BolehStokMinus").Select(p => p.Nilai).FirstOrDefault();
        return nilai == "1";
    }

    private string NomorBerikutnya(int penjualanId)
    {
        var prefix = $"TRX-{DateTime.Today:yyyyMMdd}-";
        var urut = uow.Penjualan.Count(p => p.NoTransaksi.StartsWith(prefix) && p.Id != penjualanId) + 1;
        var nomor = prefix + urut.ToString("D4");
        while (uow.Penjualan.Any(p => p.NoTransaksi == nomor && p.Id != penjualanId))
            nomor = prefix + (++urut).ToString("D4");
        return nomor;
    }

    private async Task CatatAsync(string aksi, string? detail)
    {
        if (audit is null) return;
        try { await audit.CatatAsync(aksi, detail); } catch { /* log tidak menggagalkan transaksi */ }
    }
}
