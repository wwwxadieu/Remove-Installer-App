using ClearOut.Strings;

namespace ClearOut.Models;

public enum DiskHealthStatus
{
    Ok,
    Warning,
    Unknown,
}

/// <summary>SMART status for one physical drive, as reported via WMI.</summary>
public sealed class DiskHealthInfo
{
    public required string DiskModel { get; init; }
    public required DiskHealthStatus Status { get; init; }

    public string StatusLabel => Status switch
    {
        DiskHealthStatus.Ok => AppStrings.DiskHealth_StatusOk,
        DiskHealthStatus.Warning => AppStrings.DiskHealth_StatusWarning,
        _ => AppStrings.DiskHealth_StatusUnknown,
    };
}
