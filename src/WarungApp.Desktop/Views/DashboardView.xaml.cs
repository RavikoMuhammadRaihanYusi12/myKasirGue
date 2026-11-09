using System.Windows.Controls;
using WarungApp.Desktop.ViewModels;

namespace WarungApp.Desktop.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as DashboardViewModel)!.MuatAsync();
    }
}
