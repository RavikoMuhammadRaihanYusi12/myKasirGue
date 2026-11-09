using System.Windows.Controls;
using System.Windows.Input;
using WarungApp.Desktop.ViewModels;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.Views;

public partial class OpnameView : UserControl
{
    public OpnameView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as OpnameViewModel)!.MuatAsync();
    }

    private async void CariKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is OpnameViewModel vm)
            await vm.CariBarangCommand.ExecuteAsync(null);
    }

    private void HasilDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is OpnameViewModel vm && vm.HasilTerpilih is not null)
            vm.TambahBarisCommand.Execute(null);
    }
}
