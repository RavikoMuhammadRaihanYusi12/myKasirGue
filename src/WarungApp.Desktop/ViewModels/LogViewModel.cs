using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarungApp.Core.Interfaces;

namespace WarungApp.Desktop.ViewModels;

public partial class LogViewModel(IAuditService audit) : ObservableObject
{
    public ObservableCollection<LogDto> Daftar { get; } = [];

    [ObservableProperty] private DateTime? _dari = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime? _sampai = DateTime.Today;
    [ObservableProperty] private string _pesan = "";

    public async Task MuatAsync()
    {
        var hasil = await audit.DaftarAsync(Dari, Sampai);
        Daftar.Clear();
        if (!hasil.Success)
        {
            Pesan = hasil.Error!;
            return;
        }

        Pesan = "";
        foreach (var l in hasil.Value ?? []) Daftar.Add(l);
    }

    [RelayCommand]
    private Task Muat() => MuatAsync();
}
