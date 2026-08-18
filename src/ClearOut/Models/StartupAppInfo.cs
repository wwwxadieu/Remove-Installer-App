using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using ClearOut.Strings;

namespace ClearOut.Models;

public enum StartupAppLocation
{
    RunKey,
    StartupFolder,
}

/// <summary>An app registered to launch at sign-in, from either the Run registry key or a
/// Startup shell folder.</summary>
public sealed partial class StartupAppInfo : ObservableObject
{
    public required string Name { get; init; }
    public string? Command { get; init; }
    public required StartupAppLocation Location { get; init; }

    /// <summary>Where this entry (and its StartupApproved flag) live - same hive/view as the
    /// Run value, or Registry64 + the hive the Startup folder belongs to for folder entries.</summary>
    public required RegistryHive Hive { get; init; }
    public required RegistryView View { get; init; }

    /// <summary>Run-key value name, or Startup-folder file name (e.g. "MyApp.lnk") - the key
    /// used to look up/write the StartupApproved flag for this entry.</summary>
    public required string ApprovalKeyName { get; init; }

    [ObservableProperty]
    private bool _isEnabled;

    public string LocationLabel => Location switch
    {
        StartupAppLocation.StartupFolder => AppStrings.AdvancedTools_StartupApps_LocationStartupFolder,
        _ => AppStrings.AdvancedTools_StartupApps_LocationRunKey,
    };
}
