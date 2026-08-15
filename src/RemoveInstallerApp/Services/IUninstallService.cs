using System.Threading;
using System.Threading.Tasks;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public interface IUninstallService
{
    /// <summary>Runs the app's own uninstaller (UninstallString/QuietUninstallString) and waits for it to exit.</summary>
    Task<UninstallResult> RunUninstallerAsync(InstalledAppInfo app, bool silent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manual fallback used when the app has no uninstaller (or it failed): removes the install
    /// folder, Start Menu/Desktop shortcuts referencing it, and the Uninstall registry entry itself.
    /// </summary>
    Task<UninstallResult> ForceRemoveAsync(InstalledAppInfo app, CancellationToken cancellationToken = default);
}
