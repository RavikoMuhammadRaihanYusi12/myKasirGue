using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarungApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kategori",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nama = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategori", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogAktivitas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Waktu = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Aksi = table.Column<string>(type: "TEXT", nullable: false),
                    Detail = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogAktivitas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pengaturan",
                columns: table => new
                {
                    Kunci = table.Column<string>(type: "TEXT", nullable: false),
                    Nilai = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pengaturan", x => x.Kunci);
                });

            migrationBuilder.CreateTable(
                name: "Penjualan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NoTransaksi = table.Column<string>(type: "TEXT", nullable: false),
                    Tanggal = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Total = table.Column<long>(type: "INTEGER", nullable: false),
                    Bayar = table.Column<long>(type: "INTEGER", nullable: false),
                    Kembalian = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AlasanVoid = table.Column<string>(type: "TEXT", nullable: true),
                    VoidOlehUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Penjualan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StokMasuk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tanggal = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Supplier = table.Column<string>(type: "TEXT", nullable: true),
                    Catatan = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokMasuk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    NamaLengkap = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Aktif = table.Column<bool>(type: "INTEGER", nullable: false),
                    DibuatPada = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Barang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kode = table.Column<string>(type: "TEXT", nullable: true),
                    Nama = table.Column<string>(type: "TEXT", nullable: false),
                    KategoriId = table.Column<int>(type: "INTEGER", nullable: false),
                    Satuan = table.Column<string>(type: "TEXT", nullable: false),
                    HargaBeli = table.Column<long>(type: "INTEGER", nullable: false),
                    HargaJual = table.Column<long>(type: "INTEGER", nullable: false),
                    Stok = table.Column<int>(type: "INTEGER", nullable: false),
                    StokMinimum = table.Column<int>(type: "INTEGER", nullable: false),
                    Aktif = table.Column<bool>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Barang", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Barang_Kategori_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "Kategori",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MutasiStok",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BarangId = table.Column<int>(type: "INTEGER", nullable: false),
                    Jenis = table.Column<int>(type: "INTEGER", nullable: false),
                    Qty = table.Column<int>(type: "INTEGER", nullable: false),
                    StokSebelum = table.Column<int>(type: "INTEGER", nullable: false),
                    StokSesudah = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferensiId = table.Column<int>(type: "INTEGER", nullable: true),
                    Waktu = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutasiStok", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MutasiStok_Barang_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PenjualanItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PenjualanId = table.Column<int>(type: "INTEGER", nullable: false),
                    BarangId = table.Column<int>(type: "INTEGER", nullable: false),
                    Qty = table.Column<int>(type: "INTEGER", nullable: false),
                    HargaJual = table.Column<long>(type: "INTEGER", nullable: false),
                    HargaBeli = table.Column<long>(type: "INTEGER", nullable: false),
                    Subtotal = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PenjualanItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PenjualanItem_Barang_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PenjualanItem_Penjualan_PenjualanId",
                        column: x => x.PenjualanId,
                        principalTable: "Penjualan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Penyesuaian",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tanggal = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BarangId = table.Column<int>(type: "INTEGER", nullable: false),
                    StokSistem = table.Column<int>(type: "INTEGER", nullable: false),
                    StokFisik = table.Column<int>(type: "INTEGER", nullable: false),
                    Selisih = table.Column<int>(type: "INTEGER", nullable: false),
                    Alasan = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Penyesuaian", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Penyesuaian_Barang_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StokMasukItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StokMasukId = table.Column<int>(type: "INTEGER", nullable: false),
                    BarangId = table.Column<int>(type: "INTEGER", nullable: false),
                    Qty = table.Column<int>(type: "INTEGER", nullable: false),
                    HargaBeli = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokMasukItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokMasukItem_Barang_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StokMasukItem_StokMasuk_StokMasukId",
                        column: x => x.StokMasukId,
                        principalTable: "StokMasuk",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Barang_KategoriId",
                table: "Barang",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_Barang_Kode",
                table: "Barang",
                column: "Kode",
                unique: true,
                filter: "\"Kode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Barang_Nama",
                table: "Barang",
                column: "Nama");

            migrationBuilder.CreateIndex(
                name: "IX_Kategori_Nama",
                table: "Kategori",
                column: "Nama",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MutasiStok_BarangId_Waktu",
                table: "MutasiStok",
                columns: new[] { "BarangId", "Waktu" });

            migrationBuilder.CreateIndex(
                name: "IX_Penjualan_NoTransaksi",
                table: "Penjualan",
                column: "NoTransaksi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Penjualan_Tanggal",
                table: "Penjualan",
                column: "Tanggal");

            migrationBuilder.CreateIndex(
                name: "IX_PenjualanItem_BarangId",
                table: "PenjualanItem",
                column: "BarangId");

            migrationBuilder.CreateIndex(
                name: "IX_PenjualanItem_PenjualanId",
                table: "PenjualanItem",
                column: "PenjualanId");

            migrationBuilder.CreateIndex(
                name: "IX_Penyesuaian_BarangId",
                table: "Penyesuaian",
                column: "BarangId");

            migrationBuilder.CreateIndex(
                name: "IX_StokMasukItem_BarangId",
                table: "StokMasukItem",
                column: "BarangId");

            migrationBuilder.CreateIndex(
                name: "IX_StokMasukItem_StokMasukId",
                table: "StokMasukItem",
                column: "StokMasukId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogAktivitas");

            migrationBuilder.DropTable(
                name: "MutasiStok");

            migrationBuilder.DropTable(
                name: "Pengaturan");

            migrationBuilder.DropTable(
                name: "PenjualanItem");

            migrationBuilder.DropTable(
                name: "Penyesuaian");

            migrationBuilder.DropTable(
                name: "StokMasukItem");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Penjualan");

            migrationBuilder.DropTable(
                name: "Barang");

            migrationBuilder.DropTable(
                name: "StokMasuk");

            migrationBuilder.DropTable(
                name: "Kategori");
        }
    }
}
