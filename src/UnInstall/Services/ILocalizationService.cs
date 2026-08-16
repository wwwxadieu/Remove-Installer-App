namespace UnInstall.Services;

public interface ILocalizationService
{
    /// <summary>Currently active UI culture, e.g. "en-US" or "vi-VN".</summary>
    string CurrentLanguage { get; }

    /// <summary>Applies a new UI culture and persists it to settings. Raises <see cref="LanguageChanged"/>.</summary>
    void SetLanguage(string cultureCode);

    /// <summary>Raised after <see cref="SetLanguage"/> so open pages can re-read localized strings.</summary>
    event EventHandler? LanguageChanged;
}
