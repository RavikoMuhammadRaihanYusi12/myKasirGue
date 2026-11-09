# WarungApp

Aplikasi desktop offline untuk inventaris & kasir warung. Dua role: **Pemilik** dan **Kasir**.

Stack: .NET 10, WPF (MVVM), EF Core, SQLite.

## Struktur

| Proyek | Isi |
|---|---|
| `src/WarungApp.Core` | Entity, interface service, logika bisnis, otorisasi (`Guard`) |
| `src/WarungApp.Data` | `AppDbContext`, migration, `UnitOfWork`, backup |
| `src/WarungApp.Desktop` | WPF UI (views + view models) |
| `tests/WarungApp.Tests` | xUnit (in-memory SQLite) |

## Jalankan saat pengembangan

```powershell
dotnet run --project src/WarungApp.Desktop
```

Login pertama kali: aplikasi menampilkan layar **Setup Akun Pemilik**.

## Build & instalasi

```powershell
# 1. Build satu file exe (self-contained, tidak butuh .NET terpasang)
dotnet publish src/WarungApp.Desktop -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist

# 2. Buat installer (butuh Inno Setup 6: winget install JRSoftware.InnoSetup)
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\WarungApp.iss
```

Hasil: `installer-output\WarungApp-Setup-1.0.0.exe`.

## Lokasi data

| Data | Lokasi |
|---|---|
| Database | `%LOCALAPPDATA%\WarungApp\warung.db` |
| Log | `%LOCALAPPDATA%\WarungApp\logs\log-YYYYMMDD.txt` (retensi 30 hari) |
| Backup | `%LOCALAPPDATA%\WarungApp\backup\warung-yyyyMMdd-HHmmss.db` (bisa diubah di Pengaturan) |

Data **tidak terhapus** saat uninstall maupun upgrade.

Backup otomatis dibuat saat aplikasi dibuka (sekali per hari) dan saat ditutup. Rotasi mengikuti retensi di Pengaturan (default 14 file).

## Test

```powershell
dotnet test
```

## Catatan

- Semua nominal uang disimpan sebagai integer rupiah; qty integer.
- Otorisasi ditegakkan di lapisan service, bukan hanya disembunyikan di UI. Kasir tidak pernah menerima data `HargaBeli` karena DTO-nya terpisah.
- Role Kasir: layar Kasir + daftar transaksi miliknya hari ini (tanpa harga beli & laba).
- Aturan stok minus bisa diaktifkan/dimatikan di **Pengaturan → Aturan Penjualan**.
