using WarungApp.Core.Common;
using WarungApp.Core.Entities;
using WarungApp.Core.Interfaces;

namespace WarungApp.Core.Services;

public class SettingsService(IUnitOfWork uow, ICurrentUser current) : ISettingsService
{
    public const string KeyNamaToko = "NamaToko";
    public const string KeyAlamat = "Alamat";
    public const string KeyTelepon = "Telepon";
    public const string KeyBolehStokMinus = "BolehStokMinus";
    public const string KeyFolderBackup = "FolderBackup";
    public const string KeyRetensiBackup = "RetensiBackup";
    public const int RetensiDefault = 14;

    public Task<PengaturanDto> AmbilAsync()
    {
        var map = uow.Pengaturan.ToDictionary(p => p.Kunci, p => p.Nilai);
        string Get(string key, string def = "") => map.GetValueOrDefault(key, def);

        return Task.FromResult(new PengaturanDto(
            Get(KeyNamaToko, "Warung Saya"),
            Get(KeyAlamat),
            Get(KeyTelepon),
            Get(KeyBolehStokMinus) == "1",
            Get(KeyFolderBackup),
            int.TryParse(Get(KeyRetensiBackup), out var r) && r > 0 ? r : RetensiDefault));
    }

    public async Task<Result> SimpanAsync(PengaturanDto dto)
    {
        if (!Guard.BolehPemilik(current, out var error)) return Result.Fail(error!);
        if (dto.RetensiBackup < 1) return Result.Fail("Retensi backup minimal 1 file.");

        Set(KeyNamaToko, dto.NamaToko?.Trim() ?? "");
        Set(KeyAlamat, dto.Alamat?.Trim() ?? "");
        Set(KeyTelepon, dto.Telepon?.Trim() ?? "");
        Set(KeyBolehStokMinus, dto.BolehStokMinus ? "1" : "0");
        Set(KeyFolderBackup, dto.FolderBackup?.Trim() ?? "");
        Set(KeyRetensiBackup, dto.RetensiBackup.ToString());

        await uow.SaveChangesAsync();
        return Result.Ok();
    }

    private void Set(string kunci, string nilai)
    {
        var row = uow.Pengaturan.FirstOrDefault(p => p.Kunci == kunci);
        if (row is null) uow.Add(new Pengaturan { Kunci = kunci, Nilai = nilai });
        else row.Nilai = nilai;
    }
}
