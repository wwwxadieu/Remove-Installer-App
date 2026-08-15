using System.Threading;
using System.Threading.Tasks;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public interface IInstalledAppsService
{
    /// <summary>
    /// Enumerates installed applications from the Windows Uninstall registry keys
    /// (HKLM/HKCU, 32-bit and 64-bit views), skipping OS components and updates.
    /// </summary>
    Task<IReadOnlyList<InstalledAppInfo>> GetInstalledAppsAsync(CancellationToken cancellationToken = default);
}
