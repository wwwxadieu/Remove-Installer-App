using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public interface IUninstallOrchestrator
{
    /// <summary>
    /// Runs the app's own uninstaller, falls back to a manual force-remove if there isn't one
    /// (or it fails), then scans for leftovers. Shared by the windowed uninstall flow
    /// (<c>AppListViewModel</c>) and the headless "Quick uninstall" context-menu verb, so both
    /// paths behave identically.
    /// </summary>
    Task<(UninstallResult Result, IReadOnlyList<ResidueItem> Residue)> UninstallAsync(InstalledAppInfo app, CancellationToken cancellationToken = default);
}
