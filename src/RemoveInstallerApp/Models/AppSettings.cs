namespace RemoveInstallerApp.Models;

public sealed class AppSettings
{
    /// <summary>BCP-47 culture code, e.g. "en-US" or "vi-VN".</summary>
    public string Language { get; set; } = "en-US";

    /// <summary>Prefer QuietUninstallString / silent flags when running an app's own uninstaller.</summary>
    public bool PreferSilentUninstall { get; set; } = true;

    public bool IsLightTheme { get; set; }

    /// <summary>Silently check GitHub Releases for a newer version each time the app launches.</summary>
    public bool AutoCheckForUpdates { get; set; } = true;

    /// <summary>Show "Uninstall with Remove Installer App" on the right-click menu of .exe files and shortcuts.</summary>
    public bool EnableContextMenuIntegration { get; set; }

    /// <summary>
    /// The app version (informational/semver, e.g. "1.0.0-beta5") last shown to the user via the
    /// welcome/what's-new dialog. Null means the dialog has never been shown (first run).
    /// </summary>
    public string? LastSeenVersion { get; set; }
}
