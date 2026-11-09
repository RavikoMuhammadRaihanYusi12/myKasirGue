using System.Windows.Controls;
using WarungApp.Desktop.ViewModels;

namespace WarungApp.Desktop.Views;

public partial class LogView : UserControl
{
    public LogView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as LogViewModel)!.MuatAsync();
    }
}
