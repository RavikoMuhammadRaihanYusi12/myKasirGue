using WarungApp.Core.Common;
using WarungApp.Core.Entities;
using WarungApp.Core.Interfaces;

namespace WarungApp.Core.Services;

public class BarangService(IUnitOfWork uow, ICurrentUser current, IAuditService? audit = null) : IBarangService
{
    public Task<Result<List<BarangDto>>> CariAsync(string? kata, int? kategoriId, bool hanyaMenipis, bool includeNonaktif)
    {
        var q = uow.Barang.AsQueryable();

        if (!includeNonaktif) q = q.Where(b => b.Aktif);
        if (kategoriId is int kid) q = q.Where(b => b.KategoriId == kid);
        if (hanyaMenipis) q = q.Where(b => b.Stok <= b.StokMinimum);
        if (!string.IsNullOrWhiteSpace(kata))
        {
            var k = kata.Trim().ToLower();
            q = q.Where(b => b.Nama.ToLower().Contains(k) || (b.Kode != null && b.Kode.Contains(k)));
        }

        var list = q.OrderBy(b => b.Nama).Take(200)
            .Select(b => new BarangDto(b.Id, b.Kode, b.Nama, b.KategoriId, b.Kategori!.Nama, b.Satuan,
                b.HargaBeli, b.HargaJual, b.Stok, b.StokMinimum, b.Aktif))
            .ToList();

        return Task.FromResult(Result<List<BarangDto>>.Ok(list));
    }

    public Task<Result<List<BarangKasirDto>>> CariUntukKasirAsync(string? kata)
    {
        var q = uow.Barang.Where(b => b.Aktif);
        if (!string.IsNullOrWhiteSpace(kata))
        {
            var k = kata.Trim().ToLower();
            q = q.Where(b => b.Nama.ToLower().Contains(k) || (b.Kode != null && b.Kode.Contains(k)));
        }

        var list = q.OrderBy(b => b.Nama).Take(50)
            .Select(b => new BarangKasirDto(b.Id, b.Kode, b.Nama, b.Kategori!.Nama, b.Satuan, b.HargaJual, b.Stok))
            .ToList();

        return Task.FromResult(Result<List<BarangKasirDto>>.Ok(list));
    }

    public async Task<Result<BarangDto>> SimpanAsync(int? id, BarangInput input)
    {
        Guard.RequireOwner(current);

        if (string.IsNullOrWhiteSpace(input.Nama)) return Result<BarangDto>.Fail("Nama barang wajib diisi.");
        if (input.KategoriId <= 0) return Result<BarangDto>.Fail("Kategori wajib dipilih.");
        if (input.HargaJual < 0) return Result<BarangDto>.Fail("Harga jual tidak boleh negatif.");
        if (input.HargaBeli < 0) return Result<BarangDto>.Fail("Harga beli tidak boleh negatif.");
        if (input.StokMinimum < 0) return Result<BarangDto>.Fail("Stok minimum tidak boleh negatif.");

        var kode = string.IsNullOrWhiteSpace(input.Kode) ? null : input.Kode.Trim();
        if (kode is not null && uow.Barang.Any(b => b.Kode == kode && b.Id != (id ?? 0)))
            return Result<BarangDto>.Fail("Kode barang sudah dipakai barang lain.");
        if (!uow.Kategori.Any(k => k.Id == input.KategoriId))
            return Result<BarangDto>.Fail("Kategori tidak ditemukan.");

        Barang barang;
        if (id is int bid)
        {
            var existing = uow.Barang.FirstOrDefault(b => b.Id == bid);
            if (existing is null) return Result<BarangDto>.Fail("Barang tidak ditemukan.");
            barang = existing;
        }
        else
        {
            barang = new Barang { Stok = 0 };
            uow.Add(barang);
        }

        barang.Kode = kode;
        barang.Nama = input.Nama.Trim();
        barang.KategoriId = input.KategoriId;
        barang.Satuan = string.IsNullOrWhiteSpace(input.Satuan) ? "pcs" : input.Satuan.Trim();
        barang.HargaBeli = input.HargaBeli;
        barang.HargaJual = input.HargaJual;
        barang.StokMinimum = input.StokMinimum;
        barang.Aktif = input.Aktif;

        await uow.SaveChangesAsync();
        await CatatAsync("Simpan barang", $"{barang.Nama} (jual {barang.HargaJual}, beli {barang.HargaBeli}).");
        return Result<BarangDto>.Ok(ToDto(barang));
    }

    public async Task<Result> NonaktifkanAsync(int id)
    {
        Guard.RequireOwner(current);
        var barang = uow.Barang.FirstOrDefault(b => b.Id == id);
        if (barang is null) return Result.Fail("Barang tidak ditemukan.");
        barang.Aktif = false;
        await uow.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> AktifkanAsync(int id)
    {
        Guard.RequireOwner(current);
        var barang = uow.Barang.FirstOrDefault(b => b.Id == id);
        if (barang is null) return Result.Fail("Barang tidak ditemukan.");
        barang.Aktif = true;
        await uow.SaveChangesAsync();
        return Result.Ok();
    }

    public Task<Result<List<KategoriDto>>> DaftarKategoriAsync()
    {
        var list = uow.Kategori.OrderBy(k => k.Nama)
            .Select(k => new KategoriDto(k.Id, k.Nama)).ToList();
        return Task.FromResult(Result<List<KategoriDto>>.Ok(list));
    }

    public async Task<Result<int>> SimpanKategoriAsync(int? id, string nama)
    {
        Guard.RequireOwner(current);
        nama = (nama ?? "").Trim();
        if (nama.Length == 0) return Result<int>.Fail("Nama kategori wajib diisi.");
        if (uow.Kategori.Any(k => k.Nama == nama && k.Id != (id ?? 0)))
            return Result<int>.Fail("Nama kategori sudah ada.");

        if (id is int kid)
        {
            var existing = uow.Kategori.FirstOrDefault(k => k.Id == kid);
            if (existing is null) return Result<int>.Fail("Kategori tidak ditemukan.");
            existing.Nama = nama;
            await uow.SaveChangesAsync();
            return Result<int>.Ok(existing.Id);
        }

        var kategori = new Kategori { Nama = nama };
        uow.Add(kategori);
        await uow.SaveChangesAsync();
        return Result<int>.Ok(kategori.Id);
    }

    private BarangDto ToDto(Barang b)
    {
        var namaKategori = uow.Kategori.Where(k => k.Id == b.KategoriId).Select(k => k.Nama).FirstOrDefault() ?? "";
        return new BarangDto(b.Id, b.Kode, b.Nama, b.KategoriId, namaKategori, b.Satuan,
            b.HargaBeli, b.HargaJual, b.Stok, b.StokMinimum, b.Aktif);
    }

    private async Task CatatAsync(string aksi, string? detail)
    {
        if (audit is null) return;
        try { await audit.CatatAsync(aksi, detail); } catch { }
    }
}
