using ClearOut.Models;

namespace ClearOut.Services;

/// <summary>
/// Reuses IDiskCleanupService's existing drive-space and category-scan logic rather than
/// duplicating either: this only adds the two threshold checks on top.
/// </summary>
public sealed class StorageWarningService : IStorageWarningService
{
    private const double LowFreePercentThreshold = 10.0;
    private const long ExcessiveJunkBytesThreshold = 2L * 1024 * 1024 * 1024;

    private readonly IDiskCleanupService _diskCleanupService;

    public StorageWarningService(IDiskCleanupService diskCleanupService)
    {
        _diskCleanupService = diskCleanupService;
    }

    public async Task<StorageWarningResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var driveC = _diskCleanupService.GetDriveSpaceInfo()
                .FirstOrDefault(d => string.Equals(d.DriveLetter, "C:", StringComparison.OrdinalIgnoreCase));

            var freePercent = driveC is null ? 100.0 : 100.0 - driveC.UsedPercent;
            var isDriveCNearFull = driveC is not null && freePercent < LowFreePercentThreshold;

            var categories = await _diskCleanupService.ScanAsync(cancellationToken: cancellationToken);
            var junkTotalBytes = categories.Sum(c => c.SizeBytes);

            return new StorageWarningResult
            {
                IsDriveCNearFull = isDriveCNearFull,
                DriveCFreePercent = freePercent,
                IsJunkExcessive = junkTotalBytes > ExcessiveJunkBytesThreshold,
                JunkTotalBytes = junkTotalBytes,
            };
        }
        catch
        {
            return new StorageWarningResult
            {
                IsDriveCNearFull = false,
                DriveCFreePercent = 100.0,
                IsJunkExcessive = false,
                JunkTotalBytes = 0,
            };
        }
    }
}
