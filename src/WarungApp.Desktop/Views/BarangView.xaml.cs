using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WarungApp.Desktop.ViewModels;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.Views;

public partial class BarangView : UserControl
{
    public BarangView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as BarangViewModel)!.MuatAsync();
    }

    private async void CariKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is BarangViewModel vm)
            await vm.MuatAsync();
    }

    private void GridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is BarangViewModel vm && vm.TerpilihBarang is not null)
            vm.PilihCommand.Execute(vm.TerpilihBarang);
    }
}
