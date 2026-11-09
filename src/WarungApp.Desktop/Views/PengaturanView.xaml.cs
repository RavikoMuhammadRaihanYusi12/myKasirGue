using System.Windows.Controls;
using WarungApp.Desktop.ViewModels;

namespace WarungApp.Desktop.Views;

public partial class PengaturanView : UserControl
{
    public PengaturanView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as PengaturanViewModel)!.MuatAsync();
    }
}
