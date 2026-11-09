using System.Globalization;

namespace WarungApp.Core.Common;

public static class Uang
{
    public static string Format(long nilai) => nilai.ToString("N0", CultureInfo.GetCultureInfo("id-ID"));

    public static bool TryParse(string? teks, out long nilai)
    {
        nilai = 0;
        if (string.IsNullOrWhiteSpace(teks)) return false;
        var bersih = new string(teks.Where(c => char.IsDigit(c)).ToArray());
        return long.TryParse(bersih, out nilai);
    }
}

public static class Waktu
{
    public static string Format(DateTime dt) => dt.ToString("dd/MM/yyyy HH:mm");
    public static string FormatTanggal(DateTime dt) => dt.ToString("dd/MM/yyyy");
}
