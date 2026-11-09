# PRD: Aplikasi Inventaris & Kasir Warung (Desktop)

| | |
|---|---|
| **Versi** | 1.0 (Draft) |
| **Tanggal** | 8 November 2026 |
| **Platform** | Windows Desktop |
| **Tech stack** | .NET 10, WPF (MVVM), EF Core, SQLite |
| **Status** | Draft untuk review |

---

## 1. Ringkasan

Aplikasi desktop offline untuk mengelola inventaris barang dan penjualan harian toko/warung. Aplikasi mencatat stok masuk, penjualan, dan penyesuaian stok, serta menghasilkan laporan harian. Tersedia dua role: **Pemilik Toko** dan **Kasir**.

## 2. Latar Belakang & Masalah

Pencatatan warung biasanya manual (buku tulis) sehingga:
- Stok sering tidak sinkron dengan kondisi fisik.
- Barang habis atau menipis baru disadari saat ditanya pembeli.
- Omzet dan laba harian sulit dihitung akurat.
- Tidak ada jejak siapa melakukan transaksi apa.

## 3. Tujuan & Metrik Keberhasilan

| Tujuan | Metrik |
|---|---|
| Transaksi kasir cepat | Satu transaksi (3 item) selesai < 30 detik |
| Stok akurat | Selisih stok sistem vs fisik saat opname < 5% |
| Pemilik tahu kondisi toko | Laporan harian tersedia < 3 klik |
| Data aman | Backup otomatis harian berjalan tanpa intervensi |
| Mudah dipakai | Kasir baru bisa transaksi setelah < 15 menit pengenalan |

## 4. Ruang Lingkup

### 4.1 In Scope (MVP)
- Login dan manajemen user dengan 2 role
- Master kategori dan barang
- Stok masuk (kulakan)
- Penjualan/kasir
- Penyesuaian stok (opname)
- Peringatan stok menipis
- Dashboard dan laporan harian/periode
- Backup dan restore database
- Log aktivitas dasar

### 4.2 Out of Scope (Fase 2+)
- Kasbon/hutang pelanggan
- Manajemen supplier lengkap
- Integrasi barcode scanner dan printer struk thermal
- Export PDF/Excel (MVP cukup tampil di layar, export menyusul)
- Tanggal kadaluarsa per batch
- Multi-toko/multi-cabang, sinkronisasi cloud
- Versi mobile/web

## 5. Persona & Role

### 5.1 Pemilik Toko
Orang yang mengelola toko dan butuh gambaran bisnis. Akses penuh.

### 5.2 Kasir
Karyawan yang melayani pembeli. Fokus pada transaksi, akses dibatasi agar data sensitif (harga beli, laba) tidak terlihat.

### 5.3 Matriks Hak Akses

| Fitur | Pemilik | Kasir |
|---|:---:|:---:|
| Login | ✅ | ✅ |
| Layar Kasir (jual) | ✅ | ✅ |
| Lihat daftar barang & stok | ✅ | ✅ (tanpa harga beli) |
| Tambah/ubah/nonaktifkan barang | ✅ | ❌ |
| Ubah harga jual | ✅ | ❌ |
| Lihat harga beli | ✅ | ❌ |
| Stok masuk | ✅ | ❌ |
| Stok opname / penyesuaian | ✅ | ❌ |
| Dashboard omzet & laba | ✅ | ❌ |
| Laporan penjualan | ✅ | ❌ (hanya ringkasan transaksi miliknya hari ini) |
| Batalkan/void transaksi | ✅ | ❌ |
| Kelola user | ✅ | ❌ |
| Backup / restore | ✅ | ❌ |
| Pengaturan toko | ✅ | ❌ |
| Lihat log aktivitas | ✅ | ❌ |

## 6. Kebutuhan Fungsional

### FR-1 Autentikasi & User
- FR-1.1 Login dengan username dan password.
- FR-1.2 Password disimpan dalam bentuk hash (bukan teks asli).
- FR-1.3 Saat pertama kali dijalankan, aplikasi meminta pembuatan akun Pemilik.
- FR-1.4 Pemilik dapat menambah, menonaktifkan, dan reset password akun Kasir.
- FR-1.5 Tombol logout/ganti user selalu tersedia.
- FR-1.6 Menu yang tidak diizinkan untuk role tertentu disembunyikan dan juga diblokir di sisi logika (bukan hanya disembunyikan di UI).

### FR-2 Master Barang & Kategori
- FR-2.1 CRUD kategori.
- FR-2.2 CRUD barang: kode/barcode (opsional, unik), nama, kategori, satuan, harga beli, harga jual, stok, stok minimum, status aktif.
- FR-2.3 Barang tidak dihapus permanen jika sudah punya transaksi; hanya dinonaktifkan.
- FR-2.4 Pencarian barang berdasarkan nama atau kode, plus filter kategori dan stok menipis.
- FR-2.5 Validasi: harga jual tidak boleh negatif; peringatan jika harga jual < harga beli.

### FR-3 Stok Masuk
- FR-3.1 Pemilik menginput stok masuk: tanggal, nama supplier (teks bebas), catatan, daftar barang dengan qty dan harga beli.
- FR-3.2 Saat disimpan, stok barang bertambah dan harga beli terakhir diperbarui (opsional, bisa dipilih).
- FR-3.3 Riwayat stok masuk dapat dilihat dan difilter per tanggal.

### FR-4 Penjualan (Kasir)
- FR-4.1 Cari barang berdasarkan nama atau kode, tambah ke keranjang.
- FR-4.2 Ubah qty dan hapus item dari keranjang.
- FR-4.3 Total dihitung otomatis; input uang bayar, kembalian dihitung otomatis.
- FR-4.4 Saat transaksi disimpan: stok berkurang, harga jual dan harga beli saat itu disimpan di item transaksi (snapshot).
- FR-4.5 Seluruh proses simpan berjalan dalam satu database transaction (semua berhasil atau semua gagal).
- FR-4.6 Aturan stok kosong: default **blokir** penjualan jika stok tidak cukup; Pemilik dapat mengubah menjadi "boleh minus" di Pengaturan.
- FR-4.7 Setiap transaksi memiliki nomor unik (contoh: `TRX-20261108-0001`) dan mencatat kasir yang melayani.
- FR-4.8 Void transaksi hanya oleh Pemilik, wajib alasan, stok dikembalikan, transaksi tetap tercatat berstatus "void".
- FR-4.9 Dapat dioperasikan dengan keyboard (fokus otomatis ke kolom cari, Enter untuk tambah, shortcut untuk bayar).

### FR-5 Stok Opname / Penyesuaian
- FR-5.1 Pemilik memilih barang, memasukkan stok fisik; sistem menghitung selisih.
- FR-5.2 Wajib memilih alasan: rusak, hilang, kadaluarsa, koreksi input, lainnya.
- FR-5.3 Setiap penyesuaian tercatat di riwayat mutasi stok.

### FR-6 Mutasi Stok (Jejak Audit)
- FR-6.1 Setiap perubahan stok (jual, stok masuk, opname, void) menghasilkan 1 baris mutasi: barang, jenis, qty (+/-), stok sebelum, stok sesudah, waktu, user.
- FR-6.2 Pemilik dapat melihat kartu stok per barang.

### FR-7 Dashboard & Laporan (Pemilik)
- FR-7.1 Dashboard: omzet hari ini, jumlah transaksi, estimasi laba kotor, daftar barang stok menipis, 5 barang terlaris.
- FR-7.2 Laporan penjualan per periode (harian/mingguan/bulanan/custom): omzet, laba kotor, jumlah transaksi.
- FR-7.3 Laporan barang terlaris dan barang tidak laku.
- FR-7.4 Laporan nilai stok saat ini (stok × harga beli).
- FR-7.5 Kasir hanya melihat ringkasan transaksi miliknya hari ini (tanpa laba).

### FR-8 Peringatan Stok Menipis
- FR-8.1 Barang dengan stok ≤ stok minimum ditandai dan muncul di dashboard Pemilik.
- FR-8.2 Saat login, Pemilik melihat notifikasi jumlah barang menipis.

### FR-9 Backup & Restore
- FR-9.1 Backup manual satu klik ke lokasi yang dipilih.
- FR-9.2 Auto-backup harian saat aplikasi ditutup atau dibuka, ke folder yang ditentukan di Pengaturan.
- FR-9.3 Simpan maksimal N backup terakhir (default 14), yang lama dihapus otomatis.
- FR-9.4 Restore dari file backup dengan konfirmasi dan peringatan data akan tertimpa.

### FR-10 Pengaturan
- FR-10.1 Nama toko, alamat, nomor telepon (untuk header laporan/struk nanti).
- FR-10.2 Aturan penjualan saat stok kosong (blokir/boleh minus).
- FR-10.3 Lokasi dan jumlah retensi backup.

### FR-11 Log Aktivitas
- FR-11.1 Mencatat: login/logout, perubahan harga, void transaksi, opname, restore, perubahan user.
- FR-11.2 Hanya dapat dilihat Pemilik, tidak dapat diedit/dihapus dari aplikasi.

## 7. Kebutuhan Non-Fungsional

| Kategori | Kebutuhan |
|---|---|
| **Performa** | Aplikasi terbuka < 5 detik; pencarian barang < 300 ms untuk hingga 10.000 barang |
| **Keandalan** | Transaksi atomik; aplikasi tidak merusak data saat crash atau listrik padam (SQLite WAL mode) |
| **Keamanan** | Password di-hash (PBKDF2/Argon2/BCrypt); otorisasi dicek di service layer; file database tidak berisi password teks asli |
| **Usability** | Font besar, kontras jelas, bahasa Indonesia, bisa dipakai dengan keyboard |
| **Kompatibilitas** | Windows 10/11 (64-bit), RAM minimum 4 GB |
| **Offline** | Seluruh fitur berjalan tanpa internet |
| **Maintainability** | Arsitektur berlapis (Core/Data/Desktop), unit test untuk logika stok dan transaksi |
| **Observability** | Log error ke file (Serilog) untuk membantu debug |
| **Uang** | Disimpan sebagai integer rupiah, tanpa desimal |

## 8. Alur Utama (User Flow)

**Transaksi penjualan**
1. Kasir login → layar Kasir terbuka.
2. Cari/pilih barang → masuk keranjang.
3. Input uang bayar → sistem hitung kembalian.
4. Simpan → stok berkurang, transaksi tercatat, keranjang kosong siap transaksi berikutnya.

**Kulakan**
1. Pemilik buka Stok Masuk → isi supplier dan tanggal.
2. Tambah barang, qty, harga beli.
3. Simpan → stok bertambah dan mutasi tercatat.

**Opname**
1. Pemilik pilih barang → isi stok fisik.
2. Sistem tampilkan selisih → pilih alasan.
3. Simpan → stok disesuaikan dan tercatat.

## 9. Rancangan Data (Draft)

| Tabel | Kolom utama |
|---|---|
| **User** | Id, Username, PasswordHash, NamaLengkap, Role (Pemilik/Kasir), Aktif, DibuatPada |
| **Kategori** | Id, Nama |
| **Barang** | Id, Kode, Nama, KategoriId, Satuan, HargaBeli, HargaJual, Stok, StokMinimum, Aktif |
| **Penjualan** | Id, NoTransaksi, Tanggal, UserId, Total, Bayar, Kembalian, Status (Selesai/Void), AlasanVoid |
| **PenjualanItem** | Id, PenjualanId, BarangId, Qty, HargaJual, HargaBeli, Subtotal |
| **StokMasuk** | Id, Tanggal, Supplier, Catatan, UserId |
| **StokMasukItem** | Id, StokMasukId, BarangId, Qty, HargaBeli |
| **MutasiStok** | Id, BarangId, Jenis, Qty, StokSebelum, StokSesudah, ReferensiId, Waktu, UserId |
| **Penyesuaian** | Id, Tanggal, BarangId, StokSistem, StokFisik, Selisih, Alasan, UserId |
| **Pengaturan** | Kunci, Nilai |
| **LogAktivitas** | Id, Waktu, UserId, Aksi, Detail |

## 10. Arsitektur

```
WarungApp.sln
├── WarungApp.Core      → Entity, interface, service (stok, transaksi, auth)
├── WarungApp.Data      → DbContext, repository, migrations (EF Core + SQLite)
├── WarungApp.Desktop   → WPF: Views, ViewModels (MVVM)
└── WarungApp.Tests     → xUnit
```

Lokasi data: `%LOCALAPPDATA%\WarungApp\warung.db`

## 11. Asumsi & Batasan

- Dipakai di satu komputer pada satu toko; tidak ada akses bersamaan dari banyak PC.
- Pengguna (pemilik dan kasir) memakai Windows.
- Tidak ada integrasi perangkat keras pada MVP (scanner dan printer menyusul).
- Pemilik bertanggung jawab menyimpan salinan backup di luar komputer (flashdisk/cloud pribadi).

## 12. Risiko & Mitigasi

| Risiko | Dampak | Mitigasi |
|---|---|---|
| Data hilang (PC rusak) | Tinggi | Auto-backup + anjuran salin ke flashdisk |
| Lupa password Pemilik | Sedang | Prosedur reset lewat file/kode pemulihan yang disiapkan saat setup |
| Stok tidak akurat karena input telat | Sedang | Opsi stok minus + fitur opname rutin |
| Pengguna sulit beradaptasi | Sedang | UI sederhana, shortcut keyboard, uji dengan pengguna asli |
| Kasir terlalu lambat | Sedang | Optimasi alur keyboard-first, target < 30 detik/transaksi |

## 13. Rencana Rilis

| Milestone | Isi | Estimasi |
|---|---|---|
| M0 | Setup solution, DB, migration, auth & role | 2–3 hari |
| M1 | Master kategori dan barang | 2–3 hari |
| M2 | Stok masuk, opname, mutasi stok | 3–4 hari |
| M3 | Kasir + void | 3–4 hari |
| M4 | Dashboard, laporan, stok menipis | 2–3 hari |
| M5 | Backup/restore, log, pengaturan, test | 2–3 hari |
| M6 | Installer dan uji coba di toko | 1–2 hari |

Total sekitar 3–4 minggu paruh waktu.

## 14. Kriteria Penerimaan MVP

- [ ] Pemilik dan Kasir bisa login; Kasir tidak bisa mengakses menu terlarang.
- [ ] Transaksi penjualan mengurangi stok dengan benar dan tercatat di mutasi.
- [ ] Harga di transaksi lama tidak berubah saat harga barang diubah.
- [ ] Stok tidak bisa minus (jika aturan blokir aktif).
- [ ] Void mengembalikan stok dan transaksi tetap terlihat berstatus void.
- [ ] Laporan harian cocok dengan total transaksi yang diinput.
- [ ] Backup dan restore berhasil dan data setelah restore utuh.
- [ ] Aplikasi tetap konsisten setelah ditutup paksa saat proses simpan.
- [ ] Unit test untuk logika stok dan transaksi lulus.

## 15. Pertanyaan Terbuka

1. Apakah kasir boleh memberi diskon atau mengubah harga saat transaksi? (Saat ini: tidak.)
2. Apakah perlu cetak struk di MVP, atau cukup tampil di layar?
3. Apakah satu barang bisa punya lebih dari satu satuan (misal: pcs, dus)?
4. Apakah barang timbangan (kg/gram, desimal) perlu didukung sejak awal? Ini memengaruhi tipe data stok (integer vs desimal).
5. Apakah kasir boleh melakukan stok masuk saat Pemilik tidak ada?
