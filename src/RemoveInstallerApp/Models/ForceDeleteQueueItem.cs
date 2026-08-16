using CommunityToolkit.Mvvm.ComponentModel;
using RemoveInstallerApp.Strings;

namespace RemoveInstallerApp.Models;

/// <summary>One user-picked file or folder queued for the Force Delete tool.</summary>
public sealed partial class ForceDeleteQueueItem : ObservableObject
{
    public required string Path { get; init; }
    public required bool IsFolder { get; init; }
    public long? SizeBytes { get; init; }

    [ObservableProperty]
    private bool _isSelected = true;

    public string KindLabel => AppStrings.KindLabel(IsFolder ? ResidueKind.Folder : ResidueKind.File);

    public string DisplaySize => SizeBytes is null or 0
        ? string.Empty
        : FormatBytes(SizeBytes.Value);

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
