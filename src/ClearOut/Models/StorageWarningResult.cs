namespace ClearOut.Models;

/// <summary>Result of a launch-time check for low disk space on C: and/or excessive junk files.</summary>
public sealed class StorageWarningResult
{
    public required bool IsDriveCNearFull { get; init; }
    public required double DriveCFreePercent { get; init; }
    public required bool IsJunkExcessive { get; init; }
    public required long JunkTotalBytes { get; init; }

    public bool HasAnyWarning => IsDriveCNearFull || IsJunkExcessive;

    public string DisplayJunkTotal => FormatBytes(JunkTotalBytes);

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:0.#} {units[unitIndex]}";
    }
}
