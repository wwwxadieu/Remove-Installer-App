namespace ClearOut.Models;

/// <summary>Static hardware/OS specs for the current machine, gathered once (cheap, no scanning).</summary>
public sealed class DeviceSpecsInfo
{
    public required string OsDisplayName { get; init; }
    public required string OsVersionText { get; init; }
    public required string Architecture { get; init; }
    public required string MachineName { get; init; }

    public string? CpuName { get; init; }
    public required int LogicalProcessorCount { get; init; }
    public required long TotalRamBytes { get; init; }
    public string? Manufacturer { get; init; }
    public string? Model { get; init; }

    public string DisplayTotalRam => FormatBytes(TotalRamBytes);

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
