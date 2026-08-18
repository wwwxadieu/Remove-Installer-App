using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ClearOut.Models;
using ClearOut.Services;
using ClearOut.Strings;

namespace ClearOut.Views;

/// <summary>Read-only listing of each physical drive's SMART status.</summary>
public sealed partial class DiskHealthDialog : ContentDialog
{
    private readonly IDiskHealthService _diskHealthService;
    private ObservableCollection<DiskHealthInfo> Disks { get; } = new();

    public DiskHealthDialog()
    {
        InitializeComponent();

        _diskHealthService = App.Services.GetRequiredService<IDiskHealthService>();
        Title = AppStrings.DiskHealth_Title;
        DisksListView.ItemsSource = Disks;

        Opened += async (_, _) => await LoadDisksAsync();
    }

    private async Task LoadDisksAsync()
    {
        BusyRing.IsActive = true;
        try
        {
            var disks = await _diskHealthService.GetDiskHealthAsync();
            Disks.Clear();
            foreach (var disk in disks)
            {
                Disks.Add(disk);
            }

            EmptyStateText.Visibility = Disks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            BusyRing.IsActive = false;
        }
    }
}
