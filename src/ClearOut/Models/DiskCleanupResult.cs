namespace ClearOut.Models;

public sealed class DiskCleanupResult
{
    public long BytesFreed { get; init; }

    /// <summary>Files that couldn't be deleted (in use by another process, permissions, etc.) — expected
    /// and common for temp/cache sweeps, so counted rather than reported as individual errors.</summary>
    public int SkippedFileCount { get; init; }

    /// <summary>Category-level failures (e.g. couldn't empty the Recycle Bin at all).</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
