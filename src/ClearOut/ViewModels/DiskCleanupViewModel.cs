using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ClearOut.Models;
using ClearOut.Services;

namespace ClearOut.ViewModels;

public sealed partial class DiskCleanupViewModel : ObservableObject
{
    private readonly IDiskCleanupService _diskCleanupService;

    public ObservableCollection<DiskCleanupCategory> Categories { get; } = new();

    public ObservableCollection<DriveSpaceInfo> Drives { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    public long TotalSelectedBytes => Categories.Where(c => c.IsSelected).Sum(c => c.SizeBytes);

    public DiskCleanupViewModel(IDiskCleanupService diskCleanupService)
    {
        _diskCleanupService = diskCleanupService;
    }

    /// <summary>No scanning involved (just DriveInfo.GetDrives()), so unlike Categories this
    /// doesn't need IsBusy/progress — it's cheap enough to call straight from the page's Loaded
    /// handler and refresh whenever the page is shown.</summary>
    public void LoadDrives()
    {
        Drives.Clear();
        foreach (var drive in _diskCleanupService.GetDriveSpaceInfo())
        {
            Drives.Add(drive);
        }
    }

    public async Task ScanAsync(IProgress<ScanProgress>? progress = null)
    {
        IsBusy = true;
        try
        {
            var categories = await _diskCleanupService.ScanAsync(progress);
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
