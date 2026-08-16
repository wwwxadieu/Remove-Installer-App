using CommunityToolkit.Mvvm.ComponentModel;
using ClearOut.Strings;

namespace ClearOut.Models;

/// <summary>One scanned Disk Cleanup category (e.g. "Temporary files") with its computed size.</summary>
public sealed partial class DiskCleanupCategory : ObservableObject
{
    public required DiskCleanupCategoryKind Kind { get; init; }
    public long SizeBytes { get; init; }

    [ObservableProperty]
    private bool _isSelected = true;

    public string DisplayName => AppStrings.DiskCleanupCategoryName(Kind);
    public string Description => AppStrings.DiskCleanupCategoryDescription(Kind);

    public string DisplaySize => SizeBytes <= 0
        ? string.Empty
        : FormatBytes(SizeBytes);

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
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
