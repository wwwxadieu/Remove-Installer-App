using UnInstall.Models;

namespace UnInstall.Services;

/// <summary>
/// Gates the app's "Pro" features. Currently backed only by a local, unpaid trial window (see
/// <see cref="LicenseService"/>) — there is no license key issuance or payment backend yet. This
/// interface is the seam a real licensing flow would plug into later without touching callers.
/// </summary>
public interface ILicenseService
{
    LicenseTier Tier { get; }
    bool IsPro { get; }
    DateTime? TrialExpiresAtUtc { get; }

    /// <summary>Whole days left in the trial, or null if the trial was never started.</summary>
    int? TrialDaysRemaining { get; }

    /// <summary>Starts (or restarts) the local Pro trial from now.</summary>
    void StartTrial();

    /// <summary>Reverts to the Free tier immediately.</summary>
    void EndTrial();
}
