using System.Windows.Controls;
using WarungApp.Desktop.ViewModels;

namespace WarungApp.Desktop.Views;

public partial class UserView : UserControl
{
    public UserView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as UserViewModel)!.MuatAsync();
    }

    private void PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is UserViewModel vm && sender is PasswordBox box)
            vm.Password = box.Password;
    }
}
