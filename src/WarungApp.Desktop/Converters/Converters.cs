using System.Globalization;
using System.Windows.Data;
using WarungApp.Core.Common;

namespace WarungApp.Desktop.Converters;

public class RupiahConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is long l ? Uang.Format(l) : value?.ToString() ?? "";

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) => Binding.DoNothing;
}

public class BoolKeAktifConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c) => v is true ? "Aktif" : "Nonaktif";
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => Binding.DoNothing;
}

public class MenuSamaConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? parameter, CultureInfo c) =>
        string.Equals(v as string, parameter as string, StringComparison.Ordinal);

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => Binding.DoNothing;
}
