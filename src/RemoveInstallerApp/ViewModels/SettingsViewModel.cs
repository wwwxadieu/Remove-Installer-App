using CommunityToolkit.Mvvm.ComponentModel;
using RemoveInstallerApp.Models;
using RemoveInstallerApp.Services;

namespace RemoveInstallerApp.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _selectedLanguage;

    [ObservableProperty]
    private bool _preferSilentUninstall;

    public SettingsViewModel(ILocalizationService localizationService, ISettingsService settingsService)
    {
        _localizationService = localizationService;
        _settingsService = settingsService;
        _selectedLanguage = _localizationService.CurrentLanguage;
        _preferSilentUninstall = _settingsService.Current.PreferSilentUninstall;
    }

    partial void OnSelectedLanguageChanged(string value) => _localizationService.SetLanguage(value);

    partial void OnPreferSilentUninstallChanged(bool value)
    {
        _settingsService.Current.PreferSilentUninstall = value;
        _settingsService.Save();
    }

    public AppSettings Settings => _settingsService.Current;
}
