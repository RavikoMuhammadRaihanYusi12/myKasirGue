using System.Windows;

namespace WarungApp.Desktop.Views;

public partial class InputDialog : Window
{
    public string Jawaban => TxtJawaban.Text.Trim();

    public InputDialog(string pertanyaan)
    {
        InitializeComponent();
        TxtPertanyaan.Text = pertanyaan;
        Loaded += (_, _) => TxtJawaban.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (Jawaban.Length == 0)
        {
            MessageBox.Show("Isian tidak boleh kosong.", "WarungApp");
            return;
        }

        DialogResult = true;
    }

    private void Batal_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
