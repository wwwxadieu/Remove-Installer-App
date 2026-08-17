using ClearOut.Models;

namespace ClearOut.Services;

public interface IBackupService
{
    /// <summary>
    /// Creates a Windows System Restore point (best-effort). System Restore is commonly disabled
    /// on a given machine/drive, so failure is a normal, expected outcome — not an exception.
    /// </summary>
    Task<BackupResult> CreateRestorePointAsync(string description, CancellationToken cancellationToken = default);

    /// <summary>Lists existing System Restore points, newest first.</summary>
    Task<IReadOnlyList<RestorePointInfo>> GetRestorePointsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls the machine back to the given restore point. On success, Windows reboots to finish
    /// applying it — that happens outside this app's control, the same as clicking through
    /// rstrui.exe. Returns false (never throws) if the WMI call itself fails.
    /// </summary>
    Task<bool> RestoreToPointAsync(uint sequenceNumber, CancellationToken cancellationToken = default);
}
