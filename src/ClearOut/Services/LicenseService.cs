using ClearOut.Models;

namespace ClearOut.Services;

/// <summary>
/// Local-only Pro-tier gate for evaluating which advanced features are worth selling later.
/// StartTrial simply unlocks Pro on this machine for a fixed window — no license key, signature,
/// or server round-trip involved. Not a real anti-piracy mechanism; that's out of scope until
/// there's an actual payment flow to protect.
/// </summary>
public sealed class LicenseService : ILicenseService
{
    private const int TrialDurationDays = 30;

    private readonly ISettingsService _settingsService;

    public LicenseService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public LicenseTier Tier => IsPro ? LicenseTier.Pro : LicenseTier.Free;

    public bool IsPro
    {
        get
        {
            var expiresAt = TrialExpiresAtUtc;
            return expiresAt is not null && DateTime.UtcNow < expiresAt.Value;
        }
    }

    public DateTime? TrialExpiresAtUtc =>
        _settingsService.Current.LicenseTrialStartedAtUtc?.AddDays(TrialDurationDays);

    public int? TrialDaysRemaining
    {
        get
        {
            var expiresAt = TrialExpiresAtUtc;
            if (expiresAt is null)
            {
                return null;
            }

            var remaining = expiresAt.Value - DateTime.UtcNow;
            return Math.Max(0, (int)Math.Ceiling(remaining.TotalDays));
        }
    }

    public void StartTrial()
    {
        _settingsService.Current.LicenseTrialStartedAtUtc = DateTime.UtcNow;
        _settingsService.Save();
    }

    public void EndTrial()
    {
        _settingsService.Current.LicenseTrialStartedAtUtc = null;
        _settingsService.Save();
    }
}
