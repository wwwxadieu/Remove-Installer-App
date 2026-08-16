namespace UnInstall.Models;

public enum UninstallOutcome
{
    /// <summary>The app's own uninstaller ran and exited successfully.</summary>
    UninstallerSucceeded,

    /// <summary>The app's own uninstaller ran but returned a non-zero exit code.</summary>
    UninstallerFailed,

    /// <summary>No UninstallString/QuietUninstallString was present; caller should try ForceRemove.</summary>
    NoUninstallerFound,

    /// <summary>Removed manually (install folder, shortcuts, registry key) because no uninstaller existed or it failed.</summary>
    ForceRemoved,

    /// <summary>Something threw (access denied, path not found, etc).</summary>
    Error,
}

public sealed class UninstallResult
{
    public required UninstallOutcome Outcome { get; init; }
    public int? ExitCode { get; init; }
    public string? Message { get; init; }

    public bool IsSuccess =>
        Outcome is UninstallOutcome.UninstallerSucceeded or UninstallOutcome.ForceRemoved;
}
