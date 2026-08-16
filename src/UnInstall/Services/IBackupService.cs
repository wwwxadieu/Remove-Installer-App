using UnInstall.Models;

namespace UnInstall.Services;

public interface IBackupService
{
    /// <summary>
    /// Creates a Windows System Restore point (best-effort). System Restore is commonly disabled
    /// on a given machine/drive, so failure is a normal, expected outcome — not an exception.
    /// </summary>
    Task<BackupResult> CreateRestorePointAsync(string description, CancellationToken cancellationToken = default);
}
