using ClearOut.Models;

namespace ClearOut.Services;

public interface IDiskHealthService
{
    /// <summary>Reads SMART status for every physical drive. Best-effort: a drive that can't be
    /// read, or has no SMART data at all, reports as <see cref="DiskHealthStatus.Unknown"/>
    /// rather than being omitted or throwing.</summary>
    Task<IReadOnlyList<DiskHealthInfo>> GetDiskHealthAsync(CancellationToken cancellationToken = default);
}
