using System.Windows;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop;

public partial class SetupWindow : Window
{
    private readonly IAuthService _auth;

    public SetupWindow(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
        Loaded += (_, _) => TxtUsername.Focus();
    }

    private async void BtnSimpan_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Text = "";
        if (TxtPassword.Password != TxtPassword2.Password)
        {
            TxtError.Text = "Password dan ulangi password tidak sama.";
            return;
        }

        BtnSimpan.IsEnabled = false;
        try
        {
            var result = await _auth.SetupOwnerAsync(TxtUsername.Text, TxtNama.Text, TxtPassword.Password);
            if (!result.Success)
            {
                TxtError.Text = result.Error;
                return;
            }

            MessageBox.Show("Akun Pemilik berhasil dibuat. Silakan login.", "WarungApp");
            var login = new LoginWindow(_auth);
            login.Show();
            Close();
        }
        finally
        {
            BtnSimpan.IsEnabled = true;
        }
    }
}
