using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RemoveInstallerApp.Services;
using RemoveInstallerApp.Strings;
using RemoveInstallerApp.Views;

namespace RemoveInstallerApp;

public sealed partial class MainWindow : Window
{
    private string? _updateActionUrl;

    public MainWindow()
    {
        InitializeComponent();
        Title = AppStrings.AppTitle;
    }

    private void RootNavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyLocalizedLabels();
        RootNavigationView.SelectedItem = NavItemAppList;
        ContentFrame.Navigate(typeof(AppListPage));

        _ = CheckForUpdateOnLaunchAsync();
    }

    /// <summary>Re-applied after a language switch so nav labels refresh without restarting.</summary>
    public void ApplyLocalizedLabels()
    {
        NavItemAppList.Content = AppStrings.NavInstalledApps;
        NavItemResidue.Content = AppStrings.NavLeftoverCleaner;
        NavItemForceDelete.Content = AppStrings.NavForceDelete;
        NavItemDiskCleanup.Content = AppStrings.NavDiskCleanup;
        if (RootNavigationView.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = AppStrings.NavSettings;
        }
        Title = AppStrings.AppTitle;
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItemContainer is NavigationViewItem { Tag: string tag })
        {
            switch (tag)
            {
                case "apps":
                    ContentFrame.Navigate(typeof(AppListPage));
                    break;
                case "residue":
                    ContentFrame.Navigate(typeof(ResidueScanPage));
                    break;
                case "forcedelete":
                    ContentFrame.Navigate(typeof(ForceDeletePage));
                    break;
                case "diskcleanup":
                    ContentFrame.Navigate(typeof(DiskCleanupPage));
                    break;
            }
        }
    }

    private async Task CheckForUpdateOnLaunchAsync()
    {
        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        if (!settingsService.Current.AutoCheckForUpdates)
        {
            return;
        }

        var updateService = App.Services.GetRequiredService<IUpdateService>();
        var result = await updateService.CheckForUpdateAsync();

        if (result is { Success: true, IsUpdateAvailable: true })
        {
            _updateActionUrl = result.DownloadUrl ?? result.ReleaseUrl;
            UpdateInfoBar.Title = AppStrings.Settings_UpdateAvailable(result.LatestVersionText ?? string.Empty);
            UpdateActionButton.Content = AppStrings.Settings_DownloadUpdate;
            UpdateInfoBar.IsOpen = true;
        }
    }

    private void UpdateActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_updateActionUrl is not null)
        {
            Process.Start(new ProcessStartInfo { FileName = _updateActionUrl, UseShellExecute = true });
        }
    }
}
