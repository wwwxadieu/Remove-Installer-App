using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public interface IDiskCleanupService
{
    /// <summary>Computes the current size of every cleanup category. Read-only — nothing is deleted here.</summary>
    Task<IReadOnlyList<DiskCleanupCategory>> ScanAsync(CancellationToken cancellationToken = default);

    /// <summary>Cleans the given categories (best-effort; locked files are skipped, not treated as fatal).</summary>
    Task<DiskCleanupResult> CleanAsync(IEnumerable<DiskCleanupCategory> categories, CancellationToken cancellationToken = default);
}
