using System.Globalization;

namespace ClearOut.Services;

public sealed class LocalizationService : ILocalizationService
{
    private readonly ISettingsService _settingsService;

    public string CurrentLanguage => CultureInfo.CurrentUICulture.Name;

    public event EventHandler? LanguageChanged;

    public LocalizationService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        ApplyCulture(_settingsService.Current.Language);
    }

    public void SetLanguage(string cultureCode)
    {
        if (string.Equals(cultureCode, CurrentLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ApplyCulture(cultureCode);
        _settingsService.Current.Language = cultureCode;
        _settingsService.Save();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void ApplyCulture(string cultureCode)
    {
        CultureInfo culture;
        try
        {
            culture = new CultureInfo(cultureCode);
        }
        catch (CultureNotFoundException)
        {
            // A hand-edited or corrupted settings file shouldn't stop the app from starting.
            return;
        }

        // CurrentUICulture only covers the calling thread; the DefaultThread* pair is what
        // makes the choice stick on every thread created afterwards. Culture (not just
        // UICulture) is set too so dates and numbers follow the same language.
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
    }
}
