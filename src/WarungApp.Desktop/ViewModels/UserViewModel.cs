using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarungApp.Core.Common;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public partial class UserViewModel(IUserService users) : ObservableObject
{
    public ObservableCollection<UserDto> Daftar { get; } = [];

    [ObservableProperty] private UserDto? _terpilih;
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _namaLengkap = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _pesan = "";

    public async Task MuatAsync()
    {
        var hasil = await users.DaftarAsync();
        Daftar.Clear();
        foreach (var u in hasil.Value ?? []) Daftar.Add(u);
    }

    [RelayCommand]
    private void Baru()
    {
        Terpilih = null;
        Username = "";
        NamaLengkap = "";
        Password = "";
        Pesan = "";
    }

    [RelayCommand]
    private void Pilih(UserDto? dto)
    {
        Terpilih = dto;
        Username = dto?.Username ?? "";
        NamaLengkap = dto?.NamaLengkap ?? "";
        Password = "";
        Pesan = "";
    }

    [RelayCommand]
    private async Task Tambah()
    {
        var hasil = await users.TambahKasirAsync(Username, NamaLengkap, Password);
        Pesan = hasil.Success ? "Akun kasir dibuat." : hasil.Error!;
        if (hasil.Success)
        {
            await MuatAsync();
            Baru();
        }
    }

    [RelayCommand]
    private async Task ResetPassword()
    {
        if (Terpilih is null)
        {
            Pesan = "Pilih akun terlebih dahulu.";
            return;
        }

        var hasil = await users.ResetPasswordAsync(Terpilih.Id, Password);
        Pesan = hasil.Success ? "Password direset." : hasil.Error!;
    }

    [RelayCommand]
    private async Task Nonaktifkan()
    {
        if (Terpilih is null)
        {
            Pesan = "Pilih akun terlebih dahulu.";
            return;
        }

        var hasil = await users.SetAktifAsync(Terpilih.Id, false);
        Pesan = hasil.Success ? "Akun dinonaktifkan." : hasil.Error!;
        if (hasil.Success) await MuatAsync();
    }
}
