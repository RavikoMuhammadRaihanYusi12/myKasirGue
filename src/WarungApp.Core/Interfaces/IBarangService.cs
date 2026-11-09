using WarungApp.Core.Common;

namespace WarungApp.Core.Interfaces;

public record BarangDto(
    int Id, string? Kode, string Nama, int KategoriId, string Kategori,
    string Satuan, long HargaBeli, long HargaJual, int Stok, int StokMinimum, bool Aktif);

public record BarangKasirDto(int Id, string? Kode, string Nama, string Kategori, string Satuan, long HargaJual, int Stok);

public record KategoriDto(int Id, string Nama);

public record BarangInput(
    string? Kode, string Nama, int KategoriId, string Satuan,
    long HargaBeli, long HargaJual, int StokMinimum, bool Aktif = true);

public interface IBarangService
{
    Task<Result<List<BarangDto>>> CariAsync(string? kata, int? kategoriId, bool hanyaMenipis, bool includeNonaktif);
    Task<Result<List<BarangKasirDto>>> CariUntukKasirAsync(string? kata);
    Task<Result<BarangDto>> SimpanAsync(int? id, BarangInput input);
    Task<Result> NonaktifkanAsync(int id);
    Task<Result> AktifkanAsync(int id);
    Task<Result<List<KategoriDto>>> DaftarKategoriAsync();
    Task<Result<int>> SimpanKategoriAsync(int? id, string nama);
}
