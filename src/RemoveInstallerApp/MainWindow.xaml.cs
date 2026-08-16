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

        // Setting SelectedItem raises SelectionChanged, which does the navigation. Navigating
        // here as well would build AppListPage twice — two full registry scans, two Loaded
        // handlers, and two attempts to open the same startup ContentDialog.
        RootNavigationView.SelectedItem = NavItemAppList;
        if (ContentFrame.Content is null)
        {
            NavigateTo(typeof(AppListPage));
        }

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
            NavigateTo(typeof(SettingsPage));
            return;
        }

        // SelectedItemContainer is not guaranteed to be populated (it depends on whether the
        // item has been realized, the pane display mode, and whether selection came from code
        // or a click). Relying on it alone meant that when it came back null the whole handler
        // fell through silently: no navigation, no error — the pane highlight moved but the
        // content stayed put. Resolve from SelectedItem first and keep the container as a
        // fallback, then log if neither yields a tag so the failure is never invisible again.
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string
                  ?? (args.SelectedItemContainer as NavigationViewItem)?.Tag as string;

        if (tag is null)
        {
            AppLog.Warn(
                $"Navigation skipped: could not resolve a tag. " +
                $"SelectedItem={args.SelectedItem?.GetType().Name ?? "null"}, " +
                $"SelectedItemContainer={args.SelectedItemContainer?.GetType().Name ?? "null"}");
            return;
        }

        Type? pageType = tag switch
        {
            "apps" => typeof(AppListPage),
            "residue" => typeof(ResidueScanPage),
            "forcedelete" => typeof(ForceDeletePage),
            "diskcleanup" => typeof(DiskCleanupPage),
            _ => null,
        };

        if (pageType is null)
        {
            AppLog.Warn($"Navigation skipped: unknown nav tag \"{tag}\".");
            return;
        }

        NavigateTo(pageType);
    }

    /// <summary>
    /// Single navigation entry point: skips redundant re-navigation to the page already
    /// showing, and records failures. Frame.Navigate returning false, or throwing out of a
    /// page constructor, both present to the user as "the tab didn't switch" with nothing in
    /// the UI to explain it — so both are logged.
    /// </summary>
    private void NavigateTo(Type pageType)
    {
        if (ContentFrame.CurrentSourcePageType == pageType)
        {
            return;
        }

        try
        {
            if (!ContentFrame.Navigate(pageType))
            {
                AppLog.Warn($"Frame.Navigate returned false for {pageType.Name}.");
            }
        }
        catch (Exception ex)
        {
            AppLog.Error($"Navigation to {pageType.Name} threw.", ex);
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
