using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public interface IDiskCleanupService
{
    /// <summary>
    /// Computes the current size of every cleanup category. Read-only — nothing is deleted here.
    /// Reports one tick per category through <paramref name="progress"/>; sizing folders like
    /// SoftwareDistribution can take a while, so the UI needs to show which one is being measured.
    /// </summary>
    Task<IReadOnlyList<DiskCleanupCategory>> ScanAsync(
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Cleans the given categories (best-effort; locked files are skipped, not treated as fatal).</summary>
    Task<DiskCleanupResult> CleanAsync(IEnumerable<DiskCleanupCategory> categories, CancellationToken cancellationToken = default);
}
