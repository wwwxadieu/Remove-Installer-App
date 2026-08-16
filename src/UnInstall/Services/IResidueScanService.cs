using System.Threading;
using System.Threading.Tasks;
using UnInstall.Models;

namespace UnInstall.Services;

public interface IResidueScanService
{
    /// <summary>
    /// Scans well-known folders and registry locations for files/keys still referencing an
    /// app that was just uninstalled (or force-removed). Read-only — nothing is deleted here.
    /// Reports each step through <paramref name="progress"/> so the UI can show what is being
    /// examined instead of an anonymous spinner.
    /// </summary>
    Task<IReadOnlyList<ResidueItem>> ScanAfterUninstallAsync(
        InstalledAppInfo app,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// General sweep for leftovers unrelated to any single just-uninstalled app: Uninstall
    /// registry entries whose target no longer exists, and Run/RunOnce entries pointing at
    /// missing executables.
    /// </summary>
    Task<IReadOnlyList<ResidueItem>> ScanOrphanedEntriesAsync(
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the given residue items. Files/folders go to the Recycle Bin unless
    /// <paramref name="permanentlyDelete"/> is set; registry keys are always exported to a
    /// .reg backup first, since the registry has no Recycle Bin. Returns per-item errors, if any.
    /// </summary>
    Task<IReadOnlyList<string>> DeleteAsync(
        IEnumerable<ResidueItem> items,
        bool permanentlyDelete,
        CancellationToken cancellationToken = default);
}
