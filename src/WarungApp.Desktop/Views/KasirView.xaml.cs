using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WarungApp.Core.Common;
using WarungApp.Desktop.ViewModels;

namespace WarungApp.Desktop.Views;

public partial class KasirView : UserControl
{
    private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private bool _siap;

    public KasirView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is KasirViewModel vm) vm.TransaksiSukses += TampilkanStruk;
        };
        _debounce.Tick += async (_, _) =>
        {
            _debounce.Stop();
            if (DataContext is KasirViewModel vm) await vm.CariCommand.ExecuteAsync(null);
        };
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _siap = true;
        TxtCari.Focus();
        if (DataContext is KasirViewModel vm) await vm.MuatAsync();
    }

    private void CariTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_siap) return;
        _debounce.Stop();
        _debounce.Start();
    }

    private async void CariKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not KasirViewModel vm) return;

        switch (e.Key)
        {
            case Key.Enter:
                _debounce.Stop();
                await vm.CariCommand.ExecuteAsync(null);
                if (vm.HasilCari.Count > 0) vm.Tambah(vm.HasilCari[0]);
                TxtCari.Focus();
                e.Handled = true;
                break;
            case Key.F2:
                await vm.BayarCommand.ExecuteAsync(null);
                e.Handled = true;
                break;
            case Key.Escape:
                vm.KataCari = "";
                e.Handled = true;
                break;
        }
    }

    private void HasilDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is KasirViewModel vm && vm.HasilTerpilih is not null)
        {
            vm.Tambah(vm.HasilTerpilih);
            TxtCari.Focus();
        }
    }

    private void HasilKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is KasirViewModel vm && vm.HasilTerpilih is not null)
        {
            vm.Tambah(vm.HasilTerpilih);
            TxtCari.Focus();
            e.Handled = true;
        }
    }

    private async void BayarKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is KasirViewModel vm)
        {
            await vm.BayarCommand.ExecuteAsync(null);
            e.Handled = true;
        }
    }

    private void TampilkanStruk(TransaksiSuksesInfo info)
    {
        var struk = new StrukWindow(info) { Owner = Window.GetWindow(this) };
        struk.ShowDialog();
    }
}
