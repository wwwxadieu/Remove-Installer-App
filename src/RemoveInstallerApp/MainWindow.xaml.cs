using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RemoveInstallerApp.Helpers;
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

    /// <summary>
    /// Shows the welcome screen on first launch ever (LastSeenVersion unset), or a "what's new"
    /// screen with that version's GitHub release notes whenever the running version differs from
    /// the last one the user was shown. Uses the full informational version (e.g. "1.0.0-beta5"),
    /// not the numeric assembly version, so it also fires across beta-to-beta updates.
    ///
    /// Must be awaited from a Page's own Loaded handler (see AppListPage), not fired-and-forgotten
    /// from the root NavigationView's Loaded — showing a ContentDialog that early in the window's
    /// lifecycle is a known WinUI timing hazard that can leave the dialog stuck in a modal state
    /// without ever becoming interactive, which reads as the whole app being frozen/unresponsive.
    /// Sequencing it here also avoids racing AppListPage's own context-menu-launch dialog, since
    /// only one ContentDialog can be open at a time.
    /// </summary>
    public async Task ShowWelcomeOrWhatsNewIfNeededAsync()
    {
        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var currentVersion = AppVersionInfo.CurrentInformationalVersionText;
        var lastSeenVersion = settingsService.Current.LastSeenVersion;

        if (string.Equals(lastSeenVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            var dialog = new WelcomeDialog { XamlRoot = RootNavigationView.XamlRoot };

            if (string.IsNullOrEmpty(lastSeenVersion))
            {
                dialog.ConfigureAsWelcome();
            }
            else
            {
                var updateService = App.Services.GetRequiredService<IUpdateService>();
                var notes = await updateService.GetReleaseNotesAsync(currentVersion);
                dialog.ConfigureAsWhatsNew(currentVersion, notes.Success ? notes.Body : null);
            }

            await dialog.ShowAsync();
        }
        catch (Exception)
        {
            // Best-effort: a failed dialog (GitHub unreachable, a WinUI display hiccup) must
            // never block the rest of the app, and — since LastSeenVersion is still recorded
            // below — must never retry (and risk repeating the same failure) on every launch.
        }
        finally
        {
            settingsService.Current.LastSeenVersion = currentVersion;
            settingsService.Save();
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
