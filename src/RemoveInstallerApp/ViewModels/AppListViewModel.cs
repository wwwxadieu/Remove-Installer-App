using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RemoveInstallerApp.Helpers;
using RemoveInstallerApp.Models;
using RemoveInstallerApp.Services;

namespace RemoveInstallerApp.ViewModels;

public sealed partial class AppListViewModel : ObservableObject
{
    private readonly IInstalledAppsService _installedAppsService;
    private readonly IUninstallOrchestrator _uninstallOrchestrator;

    private List<InstalledAppInfo> _allApps = new();

    public ObservableCollection<InstalledAppInfo> Apps { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public AppListViewModel(
        IInstalledAppsService installedAppsService,
        IUninstallOrchestrator uninstallOrchestrator)
    {
        _installedAppsService = installedAppsService;
        _uninstallOrchestrator = uninstallOrchestrator;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var apps = await _installedAppsService.GetInstalledAppsAsync();
            _allApps = apps.ToList();
            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Runs the shared uninstall pipeline via <see cref="IUninstallOrchestrator"/> and removes
    /// the app from the list on success. The orchestrator itself is UI-agnostic and also backs
    /// the headless "Quick uninstall" context-menu verb (see <c>App.RunQuickUninstallAndExitAsync</c>).
    /// </summary>
    public async Task<(UninstallResult Result, IReadOnlyList<ResidueItem> Residue)> UninstallAppAsync(InstalledAppInfo app)
    {
        IsBusy = true;
        try
        {
            var (result, residue) = await _uninstallOrchestrator.UninstallAsync(app);

            if (result.IsSuccess)
            {
                _allApps.Remove(app);
                Apps.Remove(app);
            }

            return (result, residue);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Matches a file path (an .exe, or a shortcut's resolved target) against the loaded apps.
    /// Used by the Explorer "Uninstall with Remove Installer App" context-menu verb to jump
    /// straight to the right app.
    /// </summary>
    public InstalledAppInfo? FindByPath(string filePath) => InstalledAppMatcher.FindByPath(_allApps, filePath);

    /// <summary>
    /// Raised once after <see cref="Apps"/> has been fully repopulated. Views should refresh
    /// derived UI (empty-state, counts) from this instead of subscribing to
    /// <c>Apps.CollectionChanged</c>: a machine with several hundred installed apps would
    /// otherwise fire that handler — and a ListView layout pass — once per item, on the UI
    /// thread, on every filter keystroke.
    /// </summary>
    public event EventHandler? AppsRefreshed;

    private void ApplyFilter()
    {
        var query = string.IsNullOrWhiteSpace(SearchText)
            ? _allApps
            : _allApps.Where(a =>
                a.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (a.Publisher?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

        var filtered = query.ToList();

        // Nothing changed (common while typing a search that matches everything): skip the
        // rebuild entirely rather than clearing and refilling an identical list.
        if (filtered.Count == Apps.Count && filtered.SequenceEqual(Apps))
        {
            return;
        }

        Apps.Clear();
        foreach (var app in filtered)
        {
            Apps.Add(app);
        }

        AppsRefreshed?.Invoke(this, EventArgs.Empty);
    }
}
