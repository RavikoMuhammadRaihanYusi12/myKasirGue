# System Design: Aplikasi Inventaris & Kasir Warung (Desktop)

| | |
|---|---|
| **Versi** | 1.0 (Draft) |
| **Tanggal** | 8 November 2026 |
| **Acuan** | PRD-Aplikasi-Inventaris-Warung.md v1.0 |
| **Stack** | .NET 10, WPF, MVVM, EF Core, SQLite |

---

## 1. Tujuan Dokumen

Dokumen ini menjelaskan bagaimana kebutuhan di PRD diimplementasikan secara teknis: arsitektur, struktur proyek, desain database, alur proses penting, keamanan, penanganan error, backup, testing, dan deployment.

## 2. Gambaran Arsitektur

Aplikasi berjalan sepenuhnya di satu komputer (offline). Tidak ada server, tidak ada jaringan.

```
┌──────────────────────────────────────────────────────┐
│                  WarungApp.Desktop (WPF)             │
│   Views (XAML)  ◄──binding──►  ViewModels (MVVM)     │
└───────────────────────────┬──────────────────────────┘
                            │ memanggil
┌───────────────────────────▼──────────────────────────┐
│                  WarungApp.Core                      │
│   Services (logika bisnis)  ·  Entities  ·  Interfaces│
│   AuthService · BarangService · PenjualanService     │
│   StokService · LaporanService · BackupService       │
└───────────────────────────┬──────────────────────────┘
                            │ lewat interface (IRepository / IUnitOfWork)
┌───────────────────────────▼──────────────────────────┐
│                  WarungApp.Data                      │
│   AppDbContext (EF Core) · Repositories · Migrations │
└───────────────────────────┬──────────────────────────┘
                            │
                  ┌─────────▼─────────┐
                  │  warung.db (SQLite)│
                  └───────────────────┘
```

**Prinsip:**
- **Dependency rule**: Desktop → Core ← Data. Core tidak tahu apa pun tentang WPF maupun EF Core (hanya interface).
- **Logika bisnis hanya di Service**, bukan di ViewModel atau code-behind, supaya bisa di-unit-test.
- **Otorisasi dicek di Service**, bukan hanya menyembunyikan menu di UI.

## 3. Struktur Solution

```
WarungApp.sln
├── src/
│   ├── WarungApp.Core/
│   │   ├── Entities/          Barang, Kategori, User, Penjualan, ...
│   │   ├── Enums/             Role, JenisMutasi, StatusPenjualan
│   │   ├── Interfaces/        IBarangRepository, IUnitOfWork, ICurrentUser, ...
│   │   ├── Services/          AuthService, PenjualanService, StokService, ...
│   │   ├── Dtos/              Request/response antar layer
│   │   └── Common/            Result<T>, AppException, Guard
│   ├── WarungApp.Data/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/    Fluent API per entity
│   │   ├── Repositories/
│   │   ├── UnitOfWork.cs
│   │   └── Migrations/
│   └── WarungApp.Desktop/
│       ├── App.xaml(.cs)      Composition root (DI, Serilog, startup)
│       ├── Views/             LoginView, KasirView, BarangView, ...
│       ├── ViewModels/
│       ├── Services/          NavigationService, DialogService
│       ├── Converters/
│       └── Resources/         Styles, tema, font
└── tests/
    └── WarungApp.Tests/       xUnit: Services (pakai SQLite in-memory)
```

## 4. Komponen & Tanggung Jawab

| Komponen | Layer | Tanggung jawab |
|---|---|---|
| `AuthService` | Core | Login, hash/verify password, setup akun Pemilik pertama, kelola user |
| `CurrentUserContext` | Core | Menyimpan user yang sedang login (id, role) untuk pengecekan hak akses |
| `BarangService` | Core | CRUD barang & kategori, validasi harga, nonaktifkan barang |
| `StokService` | Core | Stok masuk, opname, penulisan mutasi stok |
| `PenjualanService` | Core | Hitung keranjang, simpan transaksi, void |
| `LaporanService` | Core | Query agregat: omzet, laba, terlaris, nilai stok |
| `BackupService` | Core | Backup/restore, rotasi backup |
| `AuditService` | Core | Menulis log aktivitas |
| `SettingsService` | Core | Baca/tulis pengaturan toko |
| `AppDbContext` | Data | Pemetaan EF Core ke SQLite |
| `UnitOfWork` | Data | Membungkus transaksi database |
| `NavigationService` | Desktop | Berpindah layar sesuai role |

## 5. Desain Database

### 5.1 Diagram Relasi (ERD)

```
User ──1:N── Penjualan ──1:N── PenjualanItem ──N:1── Barang ──N:1── Kategori
 │                                                      │
 ├──1:N── StokMasuk ──1:N── StokMasukItem ──N:1─────────┤
 ├──1:N── Penyesuaian ──────────N:1─────────────────────┤
 ├──1:N── MutasiStok ───────────N:1─────────────────────┘
 └──1:N── LogAktivitas

Pengaturan (key-value, berdiri sendiri)
```

### 5.2 Skema Tabel

**User**
| Kolom | Tipe | Catatan |
|---|---|---|
| Id | INTEGER PK | autoincrement |
| Username | TEXT | unik, tanpa spasi, case-insensitive |
| PasswordHash | TEXT | hasil PBKDF2 (lihat bagian 9) |
| NamaLengkap | TEXT | |
| Role | INTEGER | 1 = Pemilik, 2 = Kasir |
| Aktif | INTEGER | 0/1 |
| DibuatPada | TEXT | ISO 8601 UTC |

**Kategori**: Id, Nama (unik).

**Barang**
| Kolom | Tipe | Catatan |
|---|---|---|
| Id | INTEGER PK | |
| Kode | TEXT NULL | barcode/kode, unik jika terisi |
| Nama | TEXT | indeks untuk pencarian |
| KategoriId | INTEGER FK | |
| Satuan | TEXT | pcs, bungkus, dll |
| HargaBeli | INTEGER | rupiah utuh |
| HargaJual | INTEGER | rupiah utuh |
| Stok | INTEGER | lihat catatan 5.4 |
| StokMinimum | INTEGER | |
| Aktif | INTEGER | 0/1 |
| RowVersion | INTEGER | untuk deteksi konflik update |

**Penjualan**
| Kolom | Tipe | Catatan |
|---|---|---|
| Id | INTEGER PK | |
| NoTransaksi | TEXT | unik, format `TRX-yyyyMMdd-nnnn` |
| Tanggal | TEXT | waktu lokal saat transaksi |
| UserId | INTEGER FK | kasir |
| Total | INTEGER | |
| Bayar | INTEGER | |
| Kembalian | INTEGER | |
| Status | INTEGER | 1 = Selesai, 2 = Void |
| AlasanVoid | TEXT NULL | |
| VoidOlehUserId | INTEGER NULL | |

**PenjualanItem**: Id, PenjualanId, BarangId, Qty, HargaJual, HargaBeli, Subtotal. *(HargaJual dan HargaBeli adalah snapshot saat transaksi.)*

**StokMasuk**: Id, Tanggal, Supplier, Catatan, UserId.
**StokMasukItem**: Id, StokMasukId, BarangId, Qty, HargaBeli.

**Penyesuaian**: Id, Tanggal, BarangId, StokSistem, StokFisik, Selisih, Alasan, UserId.

**MutasiStok**
| Kolom | Tipe | Catatan |
|---|---|---|
| Id | INTEGER PK | |
| BarangId | INTEGER FK | indeks (BarangId, Waktu) |
| Jenis | INTEGER | StokMasuk, Penjualan, Void, Opname |
| Qty | INTEGER | positif = masuk, negatif = keluar |
| StokSebelum | INTEGER | |
| StokSesudah | INTEGER | |
| ReferensiId | INTEGER NULL | id transaksi asal |
| Waktu | TEXT | |
| UserId | INTEGER FK | |

**Pengaturan**: Kunci (PK), Nilai.
**LogAktivitas**: Id, Waktu, UserId, Aksi, Detail.

### 5.3 Indeks

- `Barang(Nama)`, `Barang(Kode)` unik parsial (hanya jika Kode tidak null)
- `Penjualan(Tanggal)`, `Penjualan(NoTransaksi)` unik
- `PenjualanItem(PenjualanId)`, `PenjualanItem(BarangId)`
- `MutasiStok(BarangId, Waktu)`
- `User(Username)` unik

### 5.4 Keputusan Desain Data

| Keputusan | Alasan |
|---|---|
| Uang disimpan `INTEGER` rupiah | Menghindari error pembulatan float/decimal |
| Kolom `Barang.Stok` disimpan langsung + `MutasiStok` sebagai jejak | Query stok cepat, dan setiap perubahan tetap bisa diaudit |
| Snapshot harga di `PenjualanItem` | Perubahan harga tidak merusak laporan lama |
| Soft delete lewat `Aktif` | Barang/user yang sudah punya transaksi tidak boleh dihapus |
| Waktu disimpan sebagai teks ISO 8601 | Pola standar EF Core + SQLite, mudah difilter per tanggal |
| Enum disimpan sebagai integer | Ringkas, tidak rentan salah ketik |

> **Catatan tentang barang timbangan:** jika nanti butuh qty desimal (kg/gram), cara paling aman adalah menyimpan qty dalam satuan terkecil sebagai integer (misal gram), bukan mengubah ke `REAL`. Keputusan ini bergantung pada jawaban pertanyaan terbuka di PRD, jadi sebaiknya diputuskan sebelum migrasi pertama dibuat.

### 5.5 Konfigurasi SQLite

- `PRAGMA journal_mode = WAL;` → tahan terhadap crash/listrik padam dan baca-tulis lebih lancar
- `PRAGMA foreign_keys = ON;` → wajib aktif di setiap koneksi (SQLite default-nya mati)
- `PRAGMA synchronous = NORMAL;` → keseimbangan keamanan dan kecepatan pada mode WAL
- `PRAGMA busy_timeout = 5000;` → mencegah error "database is locked" sesaat
- Lokasi file: `%LOCALAPPDATA%\WarungApp\warung.db`

## 6. Alur Proses Penting

### 6.1 Startup Aplikasi

```
Start
 ├─ Bangun DI container, inisialisasi Serilog
 ├─ Pastikan folder data ada
 ├─ Jalankan migrasi EF Core (Database.Migrate)
 │    └─ Jika gagal → tampilkan pesan jelas + log, aplikasi berhenti
 ├─ Auto-backup harian (jika belum ada backup hari ini)
 ├─ Ada user di tabel User?
 │    ├─ Tidak → layar Setup Akun Pemilik
 │    └─ Ya    → layar Login
 └─ Login sukses → buka shell sesuai role
```

### 6.2 Login

1. Cari user berdasarkan username (aktif saja).
2. Verifikasi password terhadap hash (perbandingan waktu-konstan).
3. Jika cocok → isi `CurrentUserContext`, catat `LogAktivitas`.
4. Jika gagal → pesan generik "Username atau password salah" (tidak membocorkan mana yang salah). Setelah 5 kali gagal berturut-turut, kunci 30 detik.

### 6.3 Simpan Transaksi Penjualan (alur paling kritis)

```
KasirViewModel.Simpan()
  └─► PenjualanService.BuatPenjualan(request)
        1. Cek role: Pemilik atau Kasir → diizinkan
        2. Validasi: keranjang tidak kosong, Bayar ≥ Total
        3. BEGIN TRANSACTION
             a. Muat semua barang di keranjang (aktif saja)
             b. Untuk tiap item:
                  - Jika aturan "blokir stok kosong" aktif dan Stok < Qty → batal, kembalikan error
                  - Kurangi Barang.Stok (update bersyarat: WHERE Stok >= Qty)
                  - Tulis MutasiStok (Jenis = Penjualan)
             c. Buat Penjualan + PenjualanItem (snapshot HargaJual & HargaBeli)
             d. Generate NoTransaksi (urut per hari)
           COMMIT
        4. Kembalikan Result.Success(NoTransaksi, Kembalian)
        Gagal di langkah mana pun → ROLLBACK, stok tidak berubah
```

Total dan kembalian **dihitung ulang di Service**, bukan dipercaya dari UI.

### 6.4 Void Transaksi

1. Cek role = Pemilik, jika bukan → tolak.
2. Transaksi harus berstatus Selesai dan alasan wajib diisi.
3. Dalam satu transaction: set Status = Void, kembalikan stok tiap item, tulis `MutasiStok` (Jenis = Void), tulis `LogAktivitas`.
4. Transaksi void tetap tampil di riwayat dan tidak dihitung di laporan omzet.

### 6.5 Stok Masuk

1. Hanya Pemilik.
2. Dalam satu transaction: simpan `StokMasuk` + item, tambah `Barang.Stok`, opsional perbarui `HargaBeli` terakhir, tulis `MutasiStok`.

### 6.6 Stok Opname

1. Hanya Pemilik. Input stok fisik, sistem hitung `Selisih = Fisik − Sistem`.
2. Dalam satu transaction: simpan `Penyesuaian`, set `Barang.Stok = StokFisik`, tulis `MutasiStok` (Jenis = Opname), tulis log.

## 7. Otorisasi (Role-Based Access)

Pengecekan dilakukan di **Service** lewat satu titik: `Guard.RequireRole(currentUser, Role.Pemilik)`.

| Service / Operasi | Pemilik | Kasir |
|---|:---:|:---:|
| `PenjualanService.BuatPenjualan` | ✅ | ✅ |
| `PenjualanService.Void` | ✅ | ❌ |
| `BarangService.Cari/Lihat` | ✅ | ✅ (DTO tanpa HargaBeli) |
| `BarangService.Tambah/Ubah/Nonaktifkan` | ✅ | ❌ |
| `StokService.*` | ✅ | ❌ |
| `LaporanService.*` | ✅ | ❌ (kecuali ringkasan hari ini milik sendiri) |
| `AuthService.KelolaUser` | ✅ | ❌ |
| `BackupService.*`, `SettingsService.Tulis` | ✅ | ❌ |
| `AuditService.Baca` | ✅ | ❌ |

**Penting:** untuk Kasir, data harga beli **tidak boleh sampai ke ViewModel**. Gunakan DTO terpisah (`BarangKasirDto`) tanpa field `HargaBeli`, bukan sekadar menyembunyikan kolom di XAML.

## 8. Desain Layer Presentasi (WPF)

- **Pola MVVM** dengan `CommunityToolkit.Mvvm` (`ObservableObject`, `[RelayCommand]`).
- **Navigasi**: satu `MainWindow` dengan `ContentControl` yang berganti ViewModel; menu samping dibangun dari daftar yang difilter menurut role.
- **Layar:**

| Layar | Role | Catatan UI |
|---|---|---|
| Login / Setup | Semua | Enter untuk submit |
| Kasir | Semua | Fokus awal di kolom cari, daftar keranjang, total besar, shortcut `F2` bayar, `Del` hapus item |
| Data Barang | Pemilik | Grid + pencarian + filter |
| Stok Masuk | Pemilik | Form header + grid item |
| Stok Opname | Pemilik | Cari barang → isi stok fisik |
| Riwayat & Void | Pemilik | Daftar transaksi + detail |
| Dashboard & Laporan | Pemilik | Kartu ringkasan + tabel periode |
| Pengguna | Pemilik | Kelola akun Kasir |
| Pengaturan & Backup | Pemilik | Data toko, aturan stok, backup/restore |
| Log Aktivitas | Pemilik | Tabel read-only |

- **Operasi database berjalan async** (`async/await`) agar UI tidak membeku. Daftar besar memakai virtualisasi (`VirtualizingStackPanel`).
- **Pencarian barang** memakai debounce ±200 ms.

## 9. Keamanan

| Aspek | Rancangan |
|---|---|
| Hash password | PBKDF2 (`Rfc2898DeriveBytes.Pbkdf2`, SHA-256, minimal 210.000 iterasi, salt acak 16 byte per user). Format tersimpan: `iterasi.salt.hash` agar parameter bisa di-upgrade |
| Perbandingan hash | `CryptographicOperations.FixedTimeEquals` |
| Kebijakan password | Minimal 6 karakter (disesuaikan agar tidak menyulitkan pengguna warung) |
| Brute force | Jeda 30 detik setelah 5 kali gagal |
| Otorisasi | Dicek di Service, bukan hanya di UI |
| Reset password Pemilik | Kode pemulihan 1x tampil saat setup (disimpan sebagai hash), dicatat di log saat dipakai |
| File database | Tidak dienkripsi di MVP (pengguna menyimpan di PC pribadi toko). Opsi Fase 2: SQLCipher |
| SQL injection | Aman lewat EF Core (query berparameter) |
| Log | Password dan data sensitif tidak pernah ditulis ke log |

## 10. Penanganan Error & Logging

- **Result pattern** (`Result<T>`) untuk kegagalan yang bisa diprediksi (stok kurang, validasi gagal) → ditampilkan sebagai pesan ramah, tanpa exception.
- **Exception** hanya untuk kondisi tak terduga; ditangkap di handler global (`DispatcherUnhandledException`, `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`) → tampil dialog "Terjadi kesalahan" tanpa stack trace, detail masuk log.
- **Serilog** ke file: `%LOCALAPPDATA%\WarungApp\logs\log-.txt`, rolling harian, simpan 30 hari.
- **Pesan error** memakai bahasa Indonesia yang jelas dan memberi langkah selanjutnya.

## 11. Backup & Restore

**Backup**
- Menggunakan **SQLite Online Backup API** (`SqliteConnection.BackupDatabase`) atau `VACUUM INTO`, **bukan** menyalin file mentah saat DB terbuka (rawan korup di mode WAL).
- Auto-backup: saat startup jika belum ada backup hari ini → `backup-yyyyMMdd-HHmm.db`.
- Retensi: simpan N terakhir (default 14), hapus yang lebih lama.
- Setelah backup, jalankan `PRAGMA integrity_check` pada salinannya untuk memastikan valid.

**Restore**
1. Konfirmasi dengan peringatan data akan tertimpa.
2. Validasi file: bisa dibuka, `integrity_check` = ok, memiliki tabel yang diharapkan.
3. Buat backup pengaman dari DB saat ini (`pre-restore-...db`).
4. Tutup semua koneksi, ganti file, jalankan migrasi (jika backup dari versi lebih lama), mulai ulang aplikasi.

## 12. Performa

| Skenario | Strategi |
|---|---|
| Pencarian barang cepat | Indeks `Nama`, `Kode`; debounce; batasi hasil (misal 50 baris) |
| Dashboard/laporan | Query agregat di database (`GROUP BY`), bukan memuat semua baris ke memori |
| Daftar transaksi panjang | Paging per halaman/tanggal |
| Kasir terasa instan | Cache ringan daftar barang aktif di memori, di-refresh saat ada perubahan |
| Startup | Migrasi hanya jalan jika ada yang tertunda |

Target: pencarian < 300 ms untuk 10.000 barang, simpan transaksi < 200 ms.

## 13. Strategi Pengujian

| Jenis | Cakupan | Alat |
|---|---|---|
| Unit test | Hitung total/kembalian, aturan stok, void, opname, otorisasi per role, hash password | xUnit |
| Integration test | Service + EF Core dengan SQLite in-memory (bukan InMemory provider, supaya perilaku FK dan transaksi sama dengan produksi) | xUnit |
| Uji transaksi atomik | Paksa gagal di tengah proses → pastikan stok tidak berubah | xUnit |
| Uji migrasi | DB versi lama → migrasi → data tetap utuh | Skrip uji |
| UAT | Alur kasir, kulakan, opname, backup/restore dengan data nyata di PC toko | Manual + checklist PRD bagian 14 |

## 14. Deployment & Pembaruan

- **Build**: `dotnet publish -c Release -r win-x64 --self-contained true` (pengguna tidak perlu menginstal .NET).
- **Installer**: Inno Setup (sederhana, hasil satu file `Setup.exe`) atau MSIX.
- **Lokasi**: program di `Program Files\WarungApp`, data di `%LOCALAPPDATA%\WarungApp` (data tidak terhapus saat uninstall/upgrade).
- **Update versi baru**: jalankan installer baru → aplikasi membuat backup otomatis sebelum migrasi → migrasi EF Core berjalan saat pertama dibuka.
- **Versioning**: SemVer (`1.0.0`), versi ditampilkan di layar Pengaturan.

## 15. Keputusan Arsitektur (ADR Ringkas)

| # | Keputusan | Alternatif | Alasan |
|---|---|---|---|
| 1 | WPF | Avalonia, WinForms | Windows-only sesuai kebutuhan, ekosistem matang. Avalonia dipertimbangkan jika perlu Linux/Mac |
| 2 | SQLite | SQL Server Express, LiteDB | Tanpa server, satu file, mudah di-backup |
| 3 | EF Core | Dapper | Migrasi bawaan dan produktivitas; skala data kecil sehingga performa bukan kendala. Query laporan berat bisa memakai SQL mentah |
| 4 | Layered (Core/Data/Desktop) | Satu project | Logika bisnis mudah diuji dan UI bisa diganti |
| 5 | Stok disimpan + tabel mutasi | Stok dihitung murni dari mutasi | Baca cepat dan tetap punya jejak audit |
| 6 | Integer rupiah | decimal | Sederhana dan bebas masalah pembulatan |
| 7 | Otorisasi di Service | Hanya di UI | UI bisa dilewati, logika bisnis tidak |
| 8 | WAL mode | Default journal | Tahan crash dan baca-tulis lebih lancar |

## 16. Roadmap Teknis ke Fase 2

| Fitur Fase 2 | Dampak desain |
|---|---|
| Kasbon pelanggan | Tabel `Pelanggan`, `Kasbon`, `PembayaranKasbon`; kolom `PelangganId` dan metode bayar di `Penjualan` |
| Supplier lengkap | Tabel `Supplier`; `StokMasuk.SupplierId` menggantikan teks bebas |
| Barcode scanner | Umumnya meniru keyboard, cukup fokus ke kolom cari; tidak butuh perubahan arsitektur |
| Printer struk thermal | Interface `IStrukPrinter` di Core, implementasi ESC/POS di Desktop |
| Export Excel/PDF | ClosedXML dan QuestPDF di `LaporanService` |
| Satuan ganda / barang timbangan | Tabel `SatuanKonversi`; qty disimpan di satuan terkecil |
| Enkripsi database | Ganti provider ke SQLCipher |

## 17. Risiko Teknis

| Risiko | Mitigasi |
|---|---|
| Korupsi DB karena listrik padam | WAL mode, transaksi atomik, auto-backup, `integrity_check` |
| Backup berisi DB yang sedang ditulis | Pakai Online Backup API, bukan copy file |
| Migrasi gagal pada update | Backup otomatis sebelum migrasi, pesan error jelas, bisa rollback manual dari backup |
| Konflik dua transaksi berbarengan (jika aplikasi dibuka ganda) | Update stok bersyarat, `busy_timeout`, dan blokir instance ganda (named `Mutex`) |
| Kasir melihat data sensitif | DTO terpisah tanpa harga beli, otorisasi di Service |
| Jam komputer salah | Peringatan jika tanggal transaksi lebih lama dari transaksi terakhir |
