using System.Windows.Controls;
using WarungApp.Desktop.ViewModels;

namespace WarungApp.Desktop.Views;

public partial class KategoriView : UserControl
{
    public KategoriView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as KategoriViewModel)!.MuatAsync();
    }
}
