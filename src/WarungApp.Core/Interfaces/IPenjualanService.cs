using WarungApp.Core.Common;

namespace WarungApp.Core.Interfaces;

public record PenjualanItemInput(int BarangId, int Qty);
public record PenjualanInput(List<PenjualanItemInput> Items, long Bayar);

public record PenjualanItemDto(int BarangId, string Barang, int Qty, long HargaJual, long HargaBeli, long Subtotal);
public record PenjualanDto(int Id, string NoTransaksi, DateTime Tanggal, string Kasir, long Total, long Bayar,
    long Kembalian, string Status, string? AlasanVoid, List<PenjualanItemDto> Items);

public record PenjualanBerhasil(int Id, string NoTransaksi, long Total, long Kembalian, long Laba);

public interface IPenjualanService
{
    Task<Result<PenjualanBerhasil>> BuatPenjualanAsync(PenjualanInput input);
    Task<Result> VoidAsync(int penjualanId, string alasan);
    Task<Result<List<PenjualanDto>>> DaftarAsync(DateTime? dari, DateTime? sampai, bool hanyaMilikSendiri);
    Task<Result<PenjualanDto>> DetailAsync(int id);
}
