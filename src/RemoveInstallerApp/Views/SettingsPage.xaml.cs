using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
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
        SilentUninstallToggle.IsOn = ViewModel.PreferSilentUninstall;
        _initializing = false;
    }

    private void LanguageRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || LanguageRadioButtons.SelectedItem is not RadioButton { Tag: string cultureCode })
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

    private void SilentUninstallToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        ViewModel.PreferSilentUninstall = SilentUninstallToggle.IsOn;
    }
}
