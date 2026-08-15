using System.Threading;
using System.Threading.Tasks;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public interface IUpdateService
{
    /// <summary>Queries the project's GitHub Releases feed and compares it against the running app version.</summary>
    Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);
}
