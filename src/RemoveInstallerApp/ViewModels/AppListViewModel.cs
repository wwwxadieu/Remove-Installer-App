using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RemoveInstallerApp.Models;
using RemoveInstallerApp.Services;

namespace RemoveInstallerApp.ViewModels;

public sealed partial class AppListViewModel : ObservableObject
{
    private readonly IInstalledAppsService _installedAppsService;
    private readonly IUninstallService _uninstallService;
    private readonly IResidueScanService _residueScanService;
    private readonly ISettingsService _settingsService;

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
        IUninstallService uninstallService,
        IResidueScanService residueScanService,
        ISettingsService settingsService)
    {
        _installedAppsService = installedAppsService;
        _uninstallService = uninstallService;
        _residueScanService = residueScanService;
        _settingsService = settingsService;
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
    /// Runs the app's own uninstaller, falls back to a manual force-remove if there isn't one
    /// (or it fails), then scans for leftovers. Removes the app from the list on success.
    /// </summary>
    public async Task<(UninstallResult Result, IReadOnlyList<ResidueItem> Residue)> UninstallAppAsync(InstalledAppInfo app)
    {
        IsBusy = true;
        try
        {
            var result = await _uninstallService.RunUninstallerAsync(app, _settingsService.Current.PreferSilentUninstall);

            if (result.Outcome is UninstallOutcome.NoUninstallerFound or UninstallOutcome.UninstallerFailed)
            {
                result = await _uninstallService.ForceRemoveAsync(app);
            }

            IReadOnlyList<ResidueItem> residue = Array.Empty<ResidueItem>();
            if (result.IsSuccess)
            {
                residue = await _residueScanService.ScanAfterUninstallAsync(app);
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
    /// Matches a file path (an .exe, or a shortcut's resolved target) against the loaded apps by
    /// install folder, UninstallString/QuietUninstallString, or DisplayIcon. Used by the Explorer
    /// "Uninstall with Remove Installer App" context-menu verb to jump straight to the right app.
    /// </summary>
    public InstalledAppInfo? FindByPath(string filePath)
    {
        string full;
        try
        {
            full = Path.GetFullPath(filePath);
        }
        catch
        {
            return null;
        }

        var byInstallFolder = _allApps.FirstOrDefault(a =>
            !string.IsNullOrWhiteSpace(a.InstallLocation) &&
            full.StartsWith(a.InstallLocation!.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase));
        if (byInstallFolder is not null)
        {
            return byInstallFolder;
        }

        return _allApps.FirstOrDefault(a =>
            ReferencesPath(a.UninstallString, full) ||
            ReferencesPath(a.QuietUninstallString, full) ||
            ReferencesPath(a.DisplayIcon, full));
    }

    private static bool ReferencesPath(string? candidate, string fullPath) =>
        !string.IsNullOrWhiteSpace(candidate) && candidate.Contains(fullPath, StringComparison.OrdinalIgnoreCase);

    private void ApplyFilter()
    {
        Apps.Clear();
        var query = string.IsNullOrWhiteSpace(SearchText)
            ? _allApps
            : _allApps.Where(a =>
                a.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (a.Publisher?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

        foreach (var app in query)
        {
            Apps.Add(app);
        }
    }
}
