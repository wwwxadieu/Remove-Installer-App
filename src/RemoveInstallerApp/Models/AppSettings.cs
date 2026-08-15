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
}
