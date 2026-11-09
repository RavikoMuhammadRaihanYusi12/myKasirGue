using System.Windows.Controls;
using System.Windows.Input;
using WarungApp.Desktop.ViewModels;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.Views;

public partial class KartuStokView : UserControl
{
    public KartuStokView()
    {
        InitializeComponent();
    }

    private async void CariKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is KartuStokViewModel vm)
            await vm.CariCommand.ExecuteAsync(null);
    }

    private void GridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is KartuStokViewModel vm && vm.HasilTerpilih is not null)
            vm.PilihCommand.Execute(vm.HasilTerpilih);
    }
}
