using System.ServiceProcess;
using ClearOut.Strings;

namespace ClearOut.Models;

public enum ServiceStartType
{
    Automatic,
    Manual,
    Disabled,
    Other,
}

/// <summary>A Windows service, as reported by <see cref="ServiceController"/> plus its start
/// type (not exposed by ServiceController, read separately from the registry).</summary>
public sealed class WindowsServiceInfo
{
    public required string ServiceName { get; init; }
    public required string DisplayName { get; init; }
    public required ServiceControllerStatus Status { get; init; }
    public required ServiceStartType StartType { get; init; }
    public required bool CanStop { get; init; }

    public bool IsRunning => Status == ServiceControllerStatus.Running;

    public string StatusLabel => Status switch
    {
        ServiceControllerStatus.Running => AppStrings.Services_StatusRunning,
        ServiceControllerStatus.Stopped => AppStrings.Services_StatusStopped,
        ServiceControllerStatus.StartPending => AppStrings.Services_StatusStarting,
        ServiceControllerStatus.StopPending => AppStrings.Services_StatusStopping,
        _ => AppStrings.Services_StatusOther,
    };

    public string StartTypeLabel => StartType switch
    {
        ServiceStartType.Automatic => AppStrings.Services_StartTypeAutomatic,
        ServiceStartType.Manual => AppStrings.Services_StartTypeManual,
        ServiceStartType.Disabled => AppStrings.Services_StartTypeDisabled,
        _ => AppStrings.Services_StartTypeOther,
    };

    public string ServiceActionLabel => IsRunning ? AppStrings.AdvancedTools_Services_StopButton : AppStrings.AdvancedTools_Services_StartButton;

    /// <summary>Whether the action button should be enabled: stopped services are assumed
    /// startable, running ones only if <see cref="CanStop"/> says the service supports it.</summary>
    public bool CanPerformAction => !IsRunning || CanStop;
}
