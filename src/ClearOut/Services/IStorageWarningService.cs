using ClearOut.Models;

namespace ClearOut.Services;

public interface IStorageWarningService
{
    /// <summary>
    /// Checks whether the C: drive is nearly full or there's an excessive amount of junk
    /// (temp/cache/Recycle Bin/etc.) sitting around. Best-effort: any failure reads as "nothing
    /// to warn about" rather than throwing.
    /// </summary>
    Task<StorageWarningResult> CheckAsync(CancellationToken cancellationToken = default);
}
