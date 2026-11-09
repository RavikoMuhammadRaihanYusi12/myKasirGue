using System.Windows.Controls;
using System.Windows.Input;
using WarungApp.Desktop.ViewModels;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.Views;

public partial class StokMasukView : UserControl
{
    public StokMasukView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as StokMasukViewModel)!.MuatAsync();
    }

    private async void CariKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is StokMasukViewModel vm)
            await vm.CariBarangCommand.ExecuteAsync(null);
    }

    private void HasilDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is StokMasukViewModel vm && vm.HasilTerpilih is not null)
            vm.TambahItemCommand.Execute(null);
    }
}
