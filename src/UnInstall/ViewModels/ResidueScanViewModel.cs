using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UnInstall.Models;
using UnInstall.Services;

namespace UnInstall.ViewModels;

public sealed partial class ResidueScanViewModel : ObservableObject
{
    private readonly IResidueScanService _residueScanService;
    private readonly ISettingsService _settingsService;

    public ObservableCollection<ResidueItem> Items { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public ResidueScanViewModel(IResidueScanService residueScanService, ISettingsService settingsService)
    {
        _residueScanService = residueScanService;
        _settingsService = settingsService;
    }

    public void LoadItems(IEnumerable<ResidueItem> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    public async Task ScanForOrphansAsync(IProgress<ScanProgress>? progress = null)
    {
        IsBusy = true;
        try
        {
            var items = await _residueScanService.ScanOrphanedEntriesAsync(progress);
            LoadItems(items);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SelectAll(bool selected)
    {
        foreach (var item in Items)
        {
            item.IsSelected = selected;
        }
    }

    public async Task<IReadOnlyList<string>> DeleteSelectedAsync()
    {
        var selected = Items.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return Array.Empty<string>();
        }

        IsBusy = true;
        try
        {
            var errors = await _residueScanService.DeleteAsync(selected, _settingsService.Current.PermanentlyDelete);
            var failedPaths = errors.Select(e => e.Split(':')[0]).ToHashSet();

            foreach (var item in selected.Where(i => !failedPaths.Contains(i.Path)))
            {
                Items.Remove(item);
            }

            return errors;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
