namespace ClearOut.Models;

/// <summary>One fixed, ready local drive/partition and its current capacity.</summary>
public sealed class DriveSpaceInfo
{
    public required string DriveLetter { get; init; }
    public string? VolumeLabel { get; init; }
    public required long TotalBytes { get; init; }
    public required long FreeBytes { get; init; }

    public long UsedBytes => TotalBytes - FreeBytes;

    public double UsedPercent => TotalBytes <= 0 ? 0 : 100.0 * UsedBytes / TotalBytes;

    public string DisplayName => string.IsNullOrWhiteSpace(VolumeLabel)
        ? DriveLetter
        : $"{DriveLetter} ({VolumeLabel})";

    public string DisplayUsedOfTotal => $"{FormatBytes(UsedBytes)} / {FormatBytes(TotalBytes)}";

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
