using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using ClearOut.Strings;

namespace ClearOut.Models;

/// <summary>
/// A leftover file, folder, shortcut, or registry key found after uninstalling
/// (or found orphaned during a general leftover scan).
/// </summary>
public sealed partial class ResidueItem : ObservableObject
{
    public required ResidueKind Kind { get; init; }

    /// <summary>Filesystem path, or "HIVE\Sub\Key\Path" for registry items.</summary>
    public required string Path { get; init; }

    public string? Description { get; init; }
    public long? SizeBytes { get; init; }

    /// <summary>Only set for registry items; needed to actually delete the key.</summary>
    public RegistryHive? Hive { get; init; }
    public RegistryView? View { get; init; }

    /// <summary>
    /// Set only for <see cref="ResidueKind.OrphanedRunEntry"/>: <see cref="Path"/> is the Run/RunOnce
    /// key itself, and this is the single value under it that should be deleted (not the whole key).
    /// </summary>
    public string? RegistryValueName { get; init; }

    /// <summary>
    /// Checked by default. High-risk kinds are created with this explicitly set to false by
    /// the scanner (see <see cref="ResidueKindExtensions.IsHighRisk"/>) so the user has to opt
    /// into each one; Kind is a required init property and so isn't available to default it here.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected = true;

    public string KindLabel => AppStrings.KindLabel(Kind);

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
