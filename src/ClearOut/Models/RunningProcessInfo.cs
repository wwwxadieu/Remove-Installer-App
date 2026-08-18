namespace ClearOut.Models;

/// <summary>A running process and its working-set memory. Named differently from
/// System.Diagnostics.Process to avoid colliding with it.</summary>
public sealed class RunningProcessInfo
{
    public required int ProcessId { get; init; }
    public required string Name { get; init; }
    public required long WorkingSetBytes { get; init; }

    public string DisplayMemory => FormatBytes(WorkingSetBytes);

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
