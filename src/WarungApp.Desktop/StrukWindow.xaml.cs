using System.Windows;
using WarungApp.Core.Common;
using WarungApp.Desktop.ViewModels;

namespace WarungApp.Desktop;

public partial class StrukWindow : Window
{
    public StrukWindow(TransaksiSuksesInfo info)
    {
        InitializeComponent();
        TxtNoTransaksi.Text = info.NoTransaksi;
        TxtTotal.Text = Uang.Format(info.Total);
        TxtBayar.Text = Uang.Format(info.Bayar);
        TxtKembalian.Text = Uang.Format(info.Kembalian);
    }

    private void Tutup_Click(object sender, RoutedEventArgs e) => Close();
}
