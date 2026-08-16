using ClearOut.Models;

namespace ClearOut.Services;

public interface IUninstallOrchestrator
{
    /// <summary>
    /// Runs the app's own uninstaller and waits for it to finish, falling back to a manual
    /// force-remove when there isn't one (unless <c>AlwaysUseAppUninstaller</c> is set).
    ///
    /// Scanning for leftovers is deliberately NOT part of this: the scan is long enough to
    /// need its own progress UI, and callers show it separately once the uninstaller has
    /// exited (see <c>Views/PostUninstallDialog</c>).
    /// </summary>
    Task<UninstallResult> UninstallAsync(InstalledAppInfo app, CancellationToken cancellationToken = default);
}
