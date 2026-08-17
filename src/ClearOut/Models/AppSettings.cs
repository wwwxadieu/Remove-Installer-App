namespace ClearOut.Models;

public sealed class AppSettings
{
    /// <summary>BCP-47 culture code, e.g. "en-US" or "vi-VN".</summary>
    public string Language { get; set; } = "en-US";

    /// <summary>Prefer QuietUninstallString / silent flags when running an app's own uninstaller.</summary>
    public bool PreferSilentUninstall { get; set; } = true;

    /// <summary>
    /// Only ever run the app's own uninstaller. When set, a missing or failing uninstaller is
    /// reported instead of falling back to deleting the install folder/registry key manually.
    /// </summary>
    public bool AlwaysUseAppUninstaller { get; set; }

    /// <summary>
    /// Delete leftover files permanently instead of sending them to the Recycle Bin. Off by
    /// default: leftover detection is heuristic name matching, so recoverable deletes are the
    /// safer default. Registry keys have no Recycle Bin and are exported to a .reg backup
    /// either way (see <c>Helpers/RegistryBackup</c>).
    /// </summary>
    public bool PermanentlyDelete { get; set; }

    /// <summary>Light/dark preference. Defaults to following Windows.</summary>
    public ThemeMode Theme { get; set; } = ThemeMode.System;

    /// <summary>Silently check GitHub Releases for a newer version each time the app launches.</summary>
    public bool AutoCheckForUpdates { get; set; } = true;

    /// <summary>Warn on launch if the C: drive is nearly full or there's a lot of junk to clean up.</summary>
    public bool AutoCheckStorageWarnings { get; set; } = true;

    /// <summary>Show "Uninstall with ClearOut" on the right-click menu of .exe files and shortcuts.</summary>
    public bool EnableContextMenuIntegration { get; set; }

    /// <summary>
    /// The app version (informational/semver, e.g. "1.0.0-beta5") last shown to the user via the
    /// welcome/what's-new dialog. Null means the dialog has never been shown (first run).
    /// </summary>
    public string? LastSeenVersion { get; set; }

    /// <summary>
    /// UTC timestamp when the local Pro trial was started (see <c>ILicenseService</c>). Null means
    /// no trial has ever been started — the app is on the Free tier. This is a personal/local
    /// trial gate for evaluating which features to eventually sell, not a real license — there is
    /// no payment backend or key issuance yet.
    /// </summary>
    public DateTime? LicenseTrialStartedAtUtc { get; set; }
}
