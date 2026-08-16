using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using RemoveInstallerApp.Helpers;
using RemoveInstallerApp.Models;
using RemoveInstallerApp.Services;
using RemoveInstallerApp.Strings;

namespace RemoveInstallerApp.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly ISettingsService _settingsService;
    private readonly IUpdateService _updateService;
    private readonly IShellIntegrationService _shellIntegrationService;
    private readonly ILicenseService _licenseService;

    private string? _updateActionUrl;

    [ObservableProperty]
    private string _selectedLanguage;

    [ObservableProperty]
    private bool _preferSilentUninstall;

    [ObservableProperty]
    private bool _alwaysUseAppUninstaller;

    [ObservableProperty]
    private bool _permanentlyDelete;

    [ObservableProperty]
    private bool _autoCheckForUpdates;

    [ObservableProperty]
    private bool _isCheckingForUpdate;

    [ObservableProperty]
    private string? _updateStatusMessage;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private bool _enableContextMenuIntegration;

    public string CurrentVersionText => AppVersionInfo.CurrentVersionText;

    public SettingsViewModel(
        ILocalizationService localizationService,
        ISettingsService settingsService,
        IUpdateService updateService,
        IShellIntegrationService shellIntegrationService,
        ILicenseService licenseService)
    {
        _localizationService = localizationService;
        _settingsService = settingsService;
        _updateService = updateService;
        _shellIntegrationService = shellIntegrationService;
        _licenseService = licenseService;
        _selectedLanguage = _localizationService.CurrentLanguage;
        _preferSilentUninstall = _settingsService.Current.PreferSilentUninstall;
        _alwaysUseAppUninstaller = _settingsService.Current.AlwaysUseAppUninstaller;
        _permanentlyDelete = _settingsService.Current.PermanentlyDelete;
        _autoCheckForUpdates = _settingsService.Current.AutoCheckForUpdates;
        _enableContextMenuIntegration = _settingsService.Current.EnableContextMenuIntegration;
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        _localizationService.SetLanguage(value);
        _shellIntegrationService.RefreshMenuText();
    }

    partial void OnPreferSilentUninstallChanged(bool value)
    {
        _settingsService.Current.PreferSilentUninstall = value;
        _settingsService.Save();
    }

    partial void OnAlwaysUseAppUninstallerChanged(bool value)
    {
        _settingsService.Current.AlwaysUseAppUninstaller = value;
        _settingsService.Save();
    }

    partial void OnPermanentlyDeleteChanged(bool value)
    {
        _settingsService.Current.PermanentlyDelete = value;
        _settingsService.Save();
    }

    partial void OnAutoCheckForUpdatesChanged(bool value)
    {
        _settingsService.Current.AutoCheckForUpdates = value;
        _settingsService.Save();
    }

    partial void OnEnableContextMenuIntegrationChanged(bool value)
    {
        _settingsService.Current.EnableContextMenuIntegration = value;
        _settingsService.Save();

        if (value)
        {
            _shellIntegrationService.Register();
        }
        else
        {
            _shellIntegrationService.Unregister();
        }
    }

    public AppSettings Settings => _settingsService.Current;

    public async Task CheckForUpdateAsync()
    {
        IsCheckingForUpdate = true;
        UpdateStatusMessage = AppStrings.Settings_Checking;
        IsUpdateAvailable = false;
        _updateActionUrl = null;

        var result = await _updateService.CheckForUpdateAsync();

        IsCheckingForUpdate = false;

        if (!result.Success)
        {
            UpdateStatusMessage = AppStrings.Settings_UpdateCheckFailed(result.ErrorMessage ?? string.Empty);
            return;
        }

        if (result.IsUpdateAvailable)
        {
            IsUpdateAvailable = true;
            _updateActionUrl = result.DownloadUrl ?? result.ReleaseUrl;
            UpdateStatusMessage = AppStrings.Settings_UpdateAvailable(result.LatestVersionText ?? string.Empty);
        }
        else
        {
            UpdateStatusMessage = AppStrings.Settings_UpToDate;
        }
    }

    public void OpenUpdateLink()
    {
        if (_updateActionUrl is not null)
        {
            Process.Start(new ProcessStartInfo { FileName = _updateActionUrl, UseShellExecute = true });
        }
    }

    public bool IsPro => _licenseService.IsPro;

    public string LicenseStatusText => _licenseService.IsPro
        ? AppStrings.License_StatusTrial(_licenseService.TrialDaysRemaining ?? 0)
        : _licenseService.TrialExpiresAtUtc is not null
            ? AppStrings.License_StatusTrialExpired
            : AppStrings.License_StatusFree;

    public void StartTrial() => _licenseService.StartTrial();

    public void EndTrial() => _licenseService.EndTrial();
}
