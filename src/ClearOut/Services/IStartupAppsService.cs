using ClearOut.Models;

namespace ClearOut.Services;

public interface IStartupAppsService
{
    /// <summary>Lists Run-key (HKLM/HKCU x 64/32-bit) and Startup-folder (per-user + all-users)
    /// entries, with their current enabled/disabled state.</summary>
    Task<IReadOnlyList<StartupAppInfo>> GetStartupAppsAsync(CancellationToken cancellationToken = default);

    /// <summary>Flips the StartupApproved flag for one entry. Never deletes the underlying Run
    /// value or shortcut file, so this is always reversible.</summary>
    Task<bool> SetEnabledAsync(StartupAppInfo app, bool enabled, CancellationToken cancellationToken = default);
}
