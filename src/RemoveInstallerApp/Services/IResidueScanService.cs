using System.Threading;
using System.Threading.Tasks;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public interface IResidueScanService
{
    /// <summary>
    /// Scans well-known folders and registry locations for files/keys still referencing an
    /// app that was just uninstalled (or force-removed). Read-only — nothing is deleted here.
    /// </summary>
    Task<IReadOnlyList<ResidueItem>> ScanAfterUninstallAsync(InstalledAppInfo app, CancellationToken cancellationToken = default);

    /// <summary>
    /// General sweep for leftovers unrelated to any single just-uninstalled app: Uninstall
    /// registry entries whose target no longer exists, and Run/RunOnce entries pointing at
    /// missing executables.
    /// </summary>
    Task<IReadOnlyList<ResidueItem>> ScanOrphanedEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes the given residue items (files/folders/shortcuts/registry keys). Returns per-item errors, if any.</summary>
    Task<IReadOnlyList<string>> DeleteAsync(IEnumerable<ResidueItem> items, CancellationToken cancellationToken = default);
}
