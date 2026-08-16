using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RemoveInstallerApp.Models;
using RemoveInstallerApp.Services;

namespace RemoveInstallerApp.ViewModels;

public sealed partial class DiskCleanupViewModel : ObservableObject
{
    private readonly IDiskCleanupService _diskCleanupService;

    public ObservableCollection<DiskCleanupCategory> Categories { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    public long TotalSelectedBytes => Categories.Where(c => c.IsSelected).Sum(c => c.SizeBytes);

    public DiskCleanupViewModel(IDiskCleanupService diskCleanupService)
    {
        _diskCleanupService = diskCleanupService;
    }

    public async Task ScanAsync()
    {
        IsBusy = true;
        try
        {
            var categories = await _diskCleanupService.ScanAsync();
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SelectAll(bool selected)
    {
        foreach (var category in Categories)
        {
            category.IsSelected = selected;
        }
    }

    public async Task<DiskCleanupResult> CleanSelectedAsync()
    {
        var selected = Categories.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return new DiskCleanupResult();
        }

        IsBusy = true;
        try
        {
            var result = await _diskCleanupService.CleanAsync(selected);
            await ScanAsync();
            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
