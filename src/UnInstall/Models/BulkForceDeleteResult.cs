namespace UnInstall.Models;

public sealed class BulkForceDeleteResult
{
    public int DeletedCount { get; init; }
    public int ScheduledForRebootCount { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
