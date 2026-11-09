using WarungApp.Core.Common;
using WarungApp.Core.Enums;
using WarungApp.Core.Interfaces;

namespace WarungApp.Core.Services;

public class LaporanService(IUnitOfWork uow, ICurrentUser current) : ILaporanService
{
    private static (DateTime Dari, DateTime Sampai) Rentang(DateTime dari, DateTime sampai) =>
        (dari.Date, sampai.Date.AddDays(1));

    private bool IsPemilik => current.User?.Role == Role.Pemilik;

    private IQueryable<Entities.Penjualan> PenjualanSelesai(DateTime dari, DateTime sampai)
    {
        var (d, s) = Rentang(dari, sampai);
        return uow.Penjualan.Where(p => p.Status == StatusPenjualan.Selesai && p.Tanggal >= d && p.Tanggal < s);
    }

    private List<BarangTerlarisDto> HitungTerlaris(DateTime dari, DateTime sampai, int limit)
    {
        var (d, s) = Rentang(dari, sampai);
        var trx = PenjualanSelesai(dari, sampai).Select(p => p.Id).ToList();
        var items = uow.PenjualanItem.Where(i => trx.Contains(i.PenjualanId)).ToList();
        var nama = uow.Barang.ToDictionary(b => b.Id, b => b.Nama);

        return items
            .GroupBy(i => i.BarangId)
            .Select(g => new BarangTerlarisDto(
                g.Key,
                nama.GetValueOrDefault(g.Key, "-"),
                g.Sum(i => i.Qty),
                g.Sum(i => i.Subtotal),
                g.Sum(i => (i.HargaJual - i.HargaBeli) * i.Qty)))
            .OrderByDescending(x => x.Qty)
            .Take(limit)
            .ToList();
    }

    public Task<Result<DashboardDto>> DashboardAsync()
    {
        if (!IsPemilik) return Task.FromResult(Result<DashboardDto>.Fail("Akses ditolak."));

        var hariIni = DateTime.Today;
        var trx = PenjualanSelesai(hariIni, hariIni).ToList();
        var detail = uow.PenjualanItem.ToList();

        var omzet = trx.Sum(p => p.Total);
        var laba = trx.Sum(p => p.Items.Sum(i => (i.HargaJual - i.HargaBeli) * i.Qty));

        var menipis = uow.Barang.Where(b => b.Aktif && b.Stok <= b.StokMinimum)
            .OrderBy(b => b.Stok).Take(100)
            .Select(b => new StokMenipisDto(b.Id, b.Nama, b.Stok, b.StokMinimum, b.Satuan))
            .ToList();

        return Task.FromResult(Result<DashboardDto>.Ok(new DashboardDto(
            omzet, laba, trx.Count, menipis.Count, menipis, HitungTerlaris(hariIni, hariIni, 5))));
    }

    public Task<Result<RingkasanDto>> RingkasanAsync(DateTime dari, DateTime sampai)
    {
        Guard.RequireOwner(current);

        var trx = PenjualanSelesai(dari, sampai).ToList();
        var trxIds = trx.Select(p => p.Id).ToHashSet();
        var items = uow.PenjualanItem.ToList().Where(i => trxIds.Contains(i.PenjualanId)).ToList();
        var nilaiStok = uow.Barang.Where(b => b.Aktif).ToList().Sum(b => (long)b.Stok * b.HargaBeli);

        return Task.FromResult(Result<RingkasanDto>.Ok(new RingkasanDto(
            trx.Sum(p => p.Total),
            items.Sum(i => (i.HargaJual - i.HargaBeli) * i.Qty),
            trx.Count,
            nilaiStok)));
    }

    public Task<Result<List<BarangTerlarisDto>>> TerlarisAsync(DateTime dari, DateTime sampai, int limit = 20)
    {
        Guard.RequireOwner(current);
        return Task.FromResult(Result<List<BarangTerlarisDto>>.Ok(HitungTerlaris(dari, sampai, limit)));
    }

    public Task<Result<List<BarangTidakLakuDto>>> TidakLakuAsync(DateTime dari, DateTime sampai)
    {
        Guard.RequireOwner(current);

        var terlaris = HitungTerlaris(dari, sampai, int.MaxValue).Select(x => x.BarangId).ToHashSet();
        var list = uow.Barang.Where(b => b.Aktif && !terlaris.Contains(b.Id))
            .OrderBy(b => b.Nama).Take(200).ToList()
            .Select(b => new BarangTidakLakuDto(b.Id, b.Nama, b.Stok, (long)b.Stok * b.HargaBeli))
            .ToList();

        return Task.FromResult(Result<List<BarangTidakLakuDto>>.Ok(list));
    }

    public Task<Result<RingkasanKasirDto>> RingkasanKasirHariIniAsync()
    {
        if (!current.IsLoggedIn) return Task.FromResult(Result<RingkasanKasirDto>.Fail("Silakan login."));

        var hariIni = DateTime.Today;
        var trx = uow.Penjualan
            .Where(p => p.Status == StatusPenjualan.Selesai && p.UserId == current.User!.Id
                        && p.Tanggal >= hariIni && p.Tanggal < hariIni.AddDays(1))
            .ToList();

        return Task.FromResult(Result<RingkasanKasirDto>.Ok(new RingkasanKasirDto(trx.Count, trx.Sum(p => p.Total))));
    }

    public Task<Result<int>> JumlahStokMenipisAsync()
    {
        var jumlah = uow.Barang.Count(b => b.Aktif && b.Stok <= b.StokMinimum);
        return Task.FromResult(Result<int>.Ok(jumlah));
    }
}
