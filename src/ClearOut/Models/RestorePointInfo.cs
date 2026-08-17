namespace ClearOut.Models;

/// <summary>One System Restore point, as reported by the WMI SystemRestore class.</summary>
public sealed class RestorePointInfo
{
    public required uint SequenceNumber { get; init; }
    public required string Description { get; init; }
    public required DateTime CreationTime { get; init; }

    public string DisplayCreationTime => CreationTime.ToLocalTime().ToString("g");
}
