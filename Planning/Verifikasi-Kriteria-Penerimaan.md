# Verifikasi Kriteria Penerimaan MVP (PRD §14)

Tanggal verifikasi: 8 November 2026 · Build: Release · Test: **27/27 lulus**

| # | Kriteria | Status | Bukti |
|---|---|:---:|---|
| 1 | Pemilik dan Kasir bisa login; Kasir tidak bisa mengakses menu terlarang | ✅ | `MainViewModel.BuildMenu` hanya memberi 2 menu ke Kasir; `Guard.RequireOwner` menolak di service → `ForbiddenException`. Diverifikasi `AuthTests`, `BarangTests.Kasir_TidakBolehUbahBarang`, `StokTests.Kasir_TidakBolehStokMasuk`, `LaporanTests.Kasir_TidakBolehLihatLaporan` |
| 2 | Transaksi mengurangi stok & tercatat di mutasi | ✅ | `PenjualanService.BuatPenjualanAsync` → `PenjualanTests.BuatPenjualan_MengurangiStok_Dan_Mutasi` (stok 10→7, mutasi qty −3) |
| 3 | Harga transaksi lama tidak berubah saat harga barang diubah | ✅ | Snapshot `HargaJual`/`HargaBeli` per item → `PenjualanTests.SnapshotHarga_TidakBerubahSaatHargaBarangDiubah` |
| 4 | Stok tidak bisa minus (aturan blokir aktif) | ✅ | `PenjualanTests.StokKurang_Diblokir_DanStokTidakBerubah`; aturan bisa dilepas → `PengaturanTests.BolehStokMinus_Mengizinkan_PenjualanLewatStok` |
| 5 | Void mengembalikan stok, transaksi tetap terlihat berstatus void | ✅ | `PenjualanTests.Void_MengembalikanStok_Dan_StatusVoid` (stok 6→10, status `Void`, alasan tersimpan) |
| 6 | Laporan harian cocok dengan total transaksi | ✅ | `LaporanTests.Ringkasan_OmzetLabaCocok`, `LaporanTests.Void_TidakDihitungDiLaporan` |
| 7 | Backup & restore berhasil, data utuh | ✅ | Backup: SQLite Online Backup API + `integrity_check` + uji smoke (file 131 KB bersih tanpa `-wal/-shm`). Restore: validasi integritas + cek tabel `Users` + backup pengaman + `Migrate()` pasca-restore. **Uji restore end-to-end belum ada unit test otomatis** — diverifikasi manual lewat UI. |
| 8 | Aplikasi tetap konsisten setelah ditutup paksa saat simpan | ✅ | Seluruh penulisan mutlak multi-tabel dibungkus transaksi (`ExecuteInTransactionAsync`); SQLite WAL + rollback dijamin DB. Ditambah `IUnitOfWork.Reload()` agar state EF bersih setelah rollback (bug ditemukan & diperbaiki di M5). |
| 9 | Unit test logika stok dan transaksi lulus | ✅ | 27 tes: auth 3, barang 4, stok 5, penjualan 7, laporan 4, pengaturan 4 |

## Yang belum dicakup MVP (sesuai PRD §4.2 Out of Scope)

Kasbon, manajemen supplier lengkap, barcode scanner, printer thermal, export PDF/Excel, kadaluarsa per batch, multi-toko/cloud, mobile/web.

## Pertanyaan terbuka PRD §15 — keputusan implementasi saat ini

| # | Pertanyaan | Keputusan |
|---|---|---|
| 1 | Kasir boleh diskon/ubah harga? | Tidak. Harga selalu dari master barang. |
| 2 | Cetak struk? | Tidak. Cukup tampil di layar. |
| 3 | Satu barang multi-satuan? | Tidak. Satu satuan per barang. |
| 4 | Barang timbangan desimal? | Tidak. Stok integer. |
| 5 | Kasir boleh stok masuk? | Tidak. Hanya Pemilik. |
