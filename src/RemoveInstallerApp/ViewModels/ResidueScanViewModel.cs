using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RemoveInstallerApp.Models;
using RemoveInstallerApp.Services;

namespace RemoveInstallerApp.ViewModels;

public sealed partial class ResidueScanViewModel : ObservableObject
{
    private readonly IResidueScanService _residueScanService;

    public ObservableCollection<ResidueItem> Items { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public ResidueScanViewModel(IResidueScanService residueScanService)
    {
        _residueScanService = residueScanService;
    }

    public void LoadItems(IEnumerable<ResidueItem> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    public async Task ScanForOrphansAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _residueScanService.ScanOrphanedEntriesAsync();
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
            var errors = await _residueScanService.DeleteAsync(selected);
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
