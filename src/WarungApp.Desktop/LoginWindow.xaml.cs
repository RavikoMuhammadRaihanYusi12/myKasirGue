using System.Windows;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop;

public partial class LoginWindow : Window
{
    private readonly IAuthService _auth;

    public LoginWindow(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
        Loaded += (_, _) => TxtUsername.Focus();
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Text = "";
        BtnLogin.IsEnabled = false;
        try
        {
            var roleText = CmbRole.SelectedValue?.ToString() ?? "Kasir";
            var role = roleText == "Pemilik" 
                ? WarungApp.Core.Enums.Role.Pemilik 
                : WarungApp.Core.Enums.Role.Kasir;

            var result = await _auth.LoginAsync(TxtUsername.Text, TxtPassword.Password, role);
            if (!result.Success)
            {
                TxtError.Text = result.Error;
                return;
            }

            ((App)Application.Current).MasukKeAplikasi();
            Close();
        }
        finally
        {
            BtnLogin.IsEnabled = true;
        }
    }
}
