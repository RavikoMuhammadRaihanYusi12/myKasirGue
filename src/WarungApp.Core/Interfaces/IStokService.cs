using WarungApp.Core.Common;

namespace WarungApp.Core.Interfaces;

public record StokMasukItemInput(int BarangId, int Qty, long HargaBeli);
public record StokMasukInput(DateTime Tanggal, string? Supplier, string? Catatan, bool UpdateHargaBeli, List<StokMasukItemInput> Items);

public record StokMasukDto(int Id, DateTime Tanggal, string? Supplier, string? Catatan, string User, long Total, List<StokMasukItemDto> Items);
public record StokMasukItemDto(int BarangId, string Barang, int Qty, long HargaBeli, long Subtotal);

public record OpnameInput(int BarangId, int StokFisik, string Alasan);

public record MutasiDto(int Id, DateTime Waktu, string Barang, string Jenis, int Qty, int StokSebelum, int StokSesudah, string User);

public record PenyesuaianDto(int Id, DateTime Tanggal, string Barang, int StokSistem, int StokFisik, int Selisih, string Alasan, string User);

public interface IStokService
{
    Task<Result<int>> SimpanStokMasukAsync(StokMasukInput input);
    Task<Result<List<StokMasukDto>>> DaftarStokMasukAsync(DateTime? dari, DateTime? sampai);
    Task<Result<int>> SimpanOpnameAsync(OpnameInput input);
    Task<Result<List<PenyesuaianDto>>> DaftarOpnameAsync(DateTime? dari, DateTime? sampai);
    Task<Result<List<MutasiDto>>> KartuStokAsync(int barangId);
}
