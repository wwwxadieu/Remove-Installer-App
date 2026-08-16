using System.Threading;
using System.Threading.Tasks;
using ClearOut.Models;

namespace ClearOut.Services;

public interface IUpdateService
{
    /// <summary>
    /// Queries the project's GitHub Releases feed and compares the highest-versioned entry
    /// (prereleases included — every release this project publishes is currently a beta) against
    /// the running app version.
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the release notes for a specific version (matched by tag, e.g. "1.0.0-beta5" →
    /// "v1.0.0-beta5"). Used by the "what's new" screen shown after an update.
    /// </summary>
    Task<ReleaseNotesResult> GetReleaseNotesAsync(string version, CancellationToken cancellationToken = default);
}
