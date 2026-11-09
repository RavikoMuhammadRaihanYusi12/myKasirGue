using WarungApp.Core.Common;

namespace WarungApp.Core.Interfaces;

public record RingkasanDto(long Omzet, long LabaKotor, int JumlahTransaksi, long NilaiStok);

public record BarangTerlarisDto(int BarangId, string Barang, int Qty, long Omzet, long Laba);

public record BarangTidakLakuDto(int BarangId, string Barang, int Stok, long NilaiStok);

public record DashboardDto(
    long OmzetHariIni,
    long LabaHariIni,
    int TransaksiHariIni,
    int JumlahStokMenipis,
    List<StokMenipisDto> StokMenipis,
    List<BarangTerlarisDto> TerlarisHariIni);

public record StokMenipisDto(int BarangId, string Barang, int Stok, int StokMinimum, string Satuan);

public record RingkasanKasirDto(int TransaksiHariIni, long OmzetHariIni);

public interface ILaporanService
{
    Task<Result<DashboardDto>> DashboardAsync();
    Task<Result<RingkasanDto>> RingkasanAsync(DateTime dari, DateTime sampai);
    Task<Result<List<BarangTerlarisDto>>> TerlarisAsync(DateTime dari, DateTime sampai, int limit = 20);
    Task<Result<List<BarangTidakLakuDto>>> TidakLakuAsync(DateTime dari, DateTime sampai);
    Task<Result<RingkasanKasirDto>> RingkasanKasirHariIniAsync();
    Task<Result<int>> JumlahStokMenipisAsync();
}
