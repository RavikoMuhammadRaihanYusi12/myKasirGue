using System.Windows.Controls;
using WarungApp.Desktop.ViewModels;

namespace WarungApp.Desktop.Views;

public partial class LaporanView : UserControl
{
    public LaporanView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as LaporanViewModel)!.MuatAsync();
    }
}
