using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RemoveInstallerApp.Helpers;
using RemoveInstallerApp.Models;
using RemoveInstallerApp.Strings;
using RemoveInstallerApp.ViewModels;

namespace RemoveInstallerApp.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    private bool _initializing = true;

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();

        LanguageRadioButtons.SelectedItem = ViewModel.SelectedLanguage == "vi-VN" ? VietnameseOption : EnglishOption;
        ThemeRadioButtons.SelectedItem = ViewModel.Theme switch
        {
            ThemeMode.Light => ThemeLightOption,
            ThemeMode.Dark => ThemeDarkOption,
            _ => ThemeSystemOption,
        };
        SilentUninstallToggle.IsOn = ViewModel.PreferSilentUninstall;
        AlwaysUseAppUninstallerToggle.IsOn = ViewModel.AlwaysUseAppUninstaller;
        PermanentlyDeleteToggle.IsOn = ViewModel.PermanentlyDelete;
        ContextMenuToggle.IsOn = ViewModel.EnableContextMenuIntegration;
        AutoCheckUpdateToggle.IsOn = ViewModel.AutoCheckForUpdates;
        CurrentVersionText.Text = AppStrings.Settings_CurrentVersion(ViewModel.CurrentVersionText);
        RefreshLicenseSection();
        _initializing = false;
    }

    private void LanguageRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || LanguageRadioButtons.SelectedItem is not RadioButton { Tag: string cultureCode })
        {
            return;
        }

        // The reload below re-enters this handler, so it must only run when the language really
        // changed. RadioButtons applies its selection when the control realizes — after the
        // constructor, i.e. after _initializing was already cleared — so this event also fires
        // on a plain page load. Without this check that turned into an endless
        // navigate-to-self loop that saturated the UI thread and trapped the user on Settings.
        if (string.Equals(cultureCode, ViewModel.SelectedLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ViewModel.SelectedLanguage = cultureCode;

        // Nav pane labels won't re-evaluate their static x:Bind on their own; refresh them,
        // and reload this page so its own labels pick up the new language immediately.
        if (App.MainAppWindow is MainWindow mainWindow)
        {
            mainWindow.ApplyLocalizedLabels();
        }
        Frame.Navigate(typeof(SettingsPage));
    }

    private void SilentUninstallToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        ViewModel.PreferSilentUninstall = SilentUninstallToggle.IsOn;
    }

    private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || ThemeRadioButtons.SelectedItem is not RadioButton { Tag: string tag })
        {
            return;
        }

        if (!Enum.TryParse<ThemeMode>(tag, out var mode) || mode == ViewModel.Theme)
        {
            return;
        }

        ViewModel.Theme = mode;
        ThemeHelper.Apply(mode);
    }

    private void AlwaysUseAppUninstallerToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        ViewModel.AlwaysUseAppUninstaller = AlwaysUseAppUninstallerToggle.IsOn;
    }

    private void PermanentlyDeleteToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        ViewModel.PermanentlyDelete = PermanentlyDeleteToggle.IsOn;
    }

    private void ContextMenuToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        ViewModel.EnableContextMenuIntegration = ContextMenuToggle.IsOn;
    }

    private void AutoCheckUpdateToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        ViewModel.AutoCheckForUpdates = AutoCheckUpdateToggle.IsOn;
    }

    private async void CheckForUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateCheckRing.IsActive = true;
        CheckForUpdateButton.IsEnabled = false;
        DownloadUpdateButton.Visibility = Visibility.Collapsed;

        await ViewModel.CheckForUpdateAsync();

        UpdateCheckRing.IsActive = false;
        CheckForUpdateButton.IsEnabled = true;
        UpdateStatusText.Text = ViewModel.UpdateStatusMessage;
        DownloadUpdateButton.Visibility = ViewModel.IsUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
    }

    private void DownloadUpdateButton_Click(object sender, RoutedEventArgs e) => ViewModel.OpenUpdateLink();

    private void StartTrialButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.StartTrial();
        RefreshLicenseSection();
    }

    private void EndTrialButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.EndTrial();
        RefreshLicenseSection();
    }

    private void RefreshLicenseSection()
    {
        LicenseStatusText.Text = ViewModel.LicenseStatusText;
        StartTrialButton.Visibility = ViewModel.IsPro ? Visibility.Collapsed : Visibility.Visible;
        EndTrialButton.Visibility = ViewModel.IsPro ? Visibility.Visible : Visibility.Collapsed;
    }
}
