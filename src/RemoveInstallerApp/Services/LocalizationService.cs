using System.Globalization;

namespace RemoveInstallerApp.Services;

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
        var culture = new CultureInfo(cultureCode);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
