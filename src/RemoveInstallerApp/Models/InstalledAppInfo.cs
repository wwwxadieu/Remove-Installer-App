using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;

namespace RemoveInstallerApp.Models;

/// <summary>
/// One entry read from a Windows "Uninstall" registry key
/// (HKLM/HKCU × 32-bit/64-bit views).
/// </summary>
public sealed partial class InstalledAppInfo : ObservableObject
{
    public required string DisplayName { get; init; }
    public string? DisplayVersion { get; init; }
    public string? Publisher { get; init; }
    public string? InstallLocation { get; init; }
    public string? UninstallString { get; init; }
    public string? QuietUninstallString { get; init; }
    public string? DisplayIcon { get; init; }
    public long? EstimatedSizeKb { get; init; }
    public DateTime? InstallDate { get; init; }

    /// <summary>Registry sub-key name under the Uninstall key, e.g. a product GUID or app id.</summary>
    public required string RegistryKeyName { get; init; }

    public required RegistryHive Hive { get; init; }
    public required RegistryView View { get; init; }

    public bool HasUninstaller =>
        !string.IsNullOrWhiteSpace(UninstallString) || !string.IsNullOrWhiteSpace(QuietUninstallString);

    /// <summary>Full path to the "...\Uninstall\{RegistryKeyName}" key, used to reopen it for cleanup.</summary>
    public string UninstallKeyPath =>
        $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{RegistryKeyName}";

    public string Id => $"{Hive}|{View}|{RegistryKeyName}";

    /// <summary>
    /// Loaded lazily and in the background by <see cref="ViewModels.AppListViewModel"/> after
    /// the list is already showing — extracting icons synchronously for a few hundred apps is
    /// exactly the kind of UI-thread stall that made the app feel unresponsive before.
    /// Null (unset) is a normal, permanent state for apps whose icon can't be resolved.
    /// </summary>
    [ObservableProperty]
    private BitmapImage? _iconSource;

    public string DisplaySize
    {
        get
        {
            if (EstimatedSizeKb is null or 0)
            {
                return string.Empty;
            }

            double sizeMb = EstimatedSizeKb.Value / 1024.0;
            return sizeMb >= 1024
                ? $"{sizeMb / 1024:0.#} GB"
                : $"{sizeMb:0.#} MB";
        }
    }
}
