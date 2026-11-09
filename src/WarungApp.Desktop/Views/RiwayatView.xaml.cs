using System.Windows.Controls;
using WarungApp.Desktop.ViewModels;

namespace WarungApp.Desktop.Views;

public partial class RiwayatView : UserControl
{
    public RiwayatView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as RiwayatViewModel)!.MuatAsync();
    }

    private void GridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is RiwayatViewModel vm && sender is DataGrid grid)
            vm.PilihCommand.Execute(grid.SelectedItem as Core.Interfaces.PenjualanDto);
    }
}
