using ClearOut.Models;

namespace ClearOut.Services;

public interface IForceDeleteService
{
    /// <summary>
    /// Deletes each queued item, optionally wiping file contents with random data first.
    /// Every path is re-checked against <see cref="Helpers.PathSafety.IsSafeToForceDelete"/>
    /// regardless of caller — this guard is never bypassed, even in force mode.
    /// </summary>
    Task<BulkForceDeleteResult> DeleteAsync(IEnumerable<ForceDeleteQueueItem> items, bool secureDelete, CancellationToken cancellationToken = default);
}
