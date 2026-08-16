using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RemoveInstallerApp.Helpers;
using RemoveInstallerApp.Models;
using RemoveInstallerApp.Services;
using RemoveInstallerApp.Strings;
using RemoveInstallerApp.ViewModels;

namespace RemoveInstallerApp.Views;

public sealed partial class AppListPage : Page
{
    public AppListViewModel ViewModel { get; }

    private readonly IBackupService _backupService;

    /// <summary>Startup-only work belongs to the process, not the page instance, so it survives
    /// page caching and any repeat Loaded event.</summary>
    private static bool _startupFlowCompleted;

    private bool _hasLoadedApps;

    public AppListPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<AppListViewModel>();
        _backupService = App.Services.GetRequiredService<IBackupService>();
        AppsListView.ItemsSource = ViewModel.Apps;
        // Deliberately NOT Apps.CollectionChanged: that fired once per item while the list was
        // being populated. Every place that mutates Apps already refreshes the empty state.
        ViewModel.AppsRefreshed += (_, _) => UpdateEmptyState();
        Loaded += AppListPage_Loaded;
    }

    private async void AppListPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Loaded fires again every time the user navigates back here. Scanning the whole
        // registry on each visit is what made the app feel slow, so only do it once.
        if (!_hasLoadedApps)
        {
            _hasLoadedApps = true;
            await RefreshAsync();
        }

        if (_startupFlowCompleted)
        {
            return;
        }
        _startupFlowCompleted = true;

        if (App.MainAppWindow is MainWindow mainWindow)
        {
            await mainWindow.ShowWelcomeOrWhatsNewIfNeededAsync();
        }

        await HandlePendingLaunchTargetAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        SetBusy(true, AppStrings.AppList_Loading);
        await ViewModel.LoadAsync();
        SetBusy(false, null);
        UpdateEmptyState();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.SearchText = SearchTextBox.Text;
        UpdateEmptyState();
    }

    /// <summary>
    /// If the app was launched via the Explorer "Uninstall with Remove Installer App" verb,
    /// resolve the clicked file (a shortcut's target, or the .exe itself) to an installed app
    /// and jump straight into the same confirm-and-uninstall flow as clicking its row button.
    /// </summary>
    private async Task HandlePendingLaunchTargetAsync()
    {
        var targetPath = App.LaunchUninstallTargetPath;
        if (targetPath is null)
        {
            return;
        }

        App.ClearLaunchUninstallTarget();

        var resolvedPath = targetPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)
            ? ShortcutResolver.ResolveTarget(targetPath) ?? targetPath
            : targetPath;

        var app = ViewModel.FindByPath(resolvedPath);
        if (app is null)
        {
            var notFoundDialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = AppStrings.AppList_ContextMenuNoMatchTitle,
                Content = AppStrings.AppList_ContextMenuNoMatchMessage(System.IO.Path.GetFileName(resolvedPath)),
                CloseButtonText = AppStrings.Common_Close,
            };
            await notFoundDialog.ShowAsync();
            return;
        }

        await UninstallFlowAsync(app);
    }

    private async void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: InstalledAppInfo app })
        {
            return;
        }

        await UninstallFlowAsync(app);
    }

    /// <summary>Confirm → uninstall → result, shared by row buttons and the context-menu launch path.</summary>
    private async Task UninstallFlowAsync(InstalledAppInfo app)
    {
        var confirmDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.AppList_ConfirmTitle,
            Content = AppStrings.AppList_ConfirmMessage(app.DisplayName),
            PrimaryButtonText = AppStrings.Common_Yes,
            CloseButtonText = AppStrings.Common_No,
            DefaultButton = ContentDialogButton.Close,
        };

        if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        if (!await OfferBackupAsync(app))
        {
            return;
        }

        // The app's own uninstaller runs to completion here (UninstallService awaits its exit)
        // before anything else happens.
        SetBusy(true, AppStrings.AppList_Loading);
        var result = await ViewModel.UninstallAppAsync(app);
        SetBusy(false, null);
        UpdateEmptyState();

        var message = UninstallResultFormatter.Format(app.DisplayName, result);

        // Then the leftover scan runs in its own dialog, which reports progress step by step.
        // Deliberately not a Frame.Navigate: that left the nav pane pointing at this page while
        // the frame showed another, so clicking "Installed apps" did nothing.
        var cleanupDialog = new PostUninstallDialog(app, message) { XamlRoot = XamlRoot };
        await cleanupDialog.ShowAsync();
    }

    /// <summary>
    /// Mandatory before every uninstall (per the app's design, not a Settings toggle): asks the
    /// user whether to create a System Restore point first. Returns false only if the user chose
    /// not to proceed with the uninstall at all.
    /// </summary>
    private async Task<bool> OfferBackupAsync(InstalledAppInfo app)
    {
        var backupDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.Backup_ConfirmTitle,
            Content = AppStrings.Backup_ConfirmMessage(app.DisplayName),
            PrimaryButtonText = AppStrings.Common_Yes,
            CloseButtonText = AppStrings.Common_No,
            // Unlike the destructive-action dialogs elsewhere in this app (DefaultButton = Close),
            // "Yes" here is the safe/recommended choice, so it's the default on Enter.
            DefaultButton = ContentDialogButton.Primary,
        };

        if (await backupDialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return true;
        }

        SetBusy(true, AppStrings.Backup_Creating);
        var backupResult = await _backupService.CreateRestorePointAsync(AppStrings.Backup_RestorePointDescription(app.DisplayName));
        SetBusy(false, null);

        if (backupResult.Success)
        {
            return true;
        }

        var failureDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.Backup_ConfirmTitle,
            Content = AppStrings.Backup_Failed(backupResult.ErrorMessage ?? string.Empty) + "\n\n" + AppStrings.Backup_ContinueAnyway,
            PrimaryButtonText = AppStrings.Common_Yes,
            CloseButtonText = AppStrings.Common_No,
            DefaultButton = ContentDialogButton.Close,
        };

        return await failureDialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private void SetBusy(bool isBusy, string? status)
    {
        BusyRing.IsActive = isBusy;
        StatusText.Text = status ?? string.Empty;
    }

    private void UpdateEmptyState()
    {
        var isEmpty = ViewModel.Apps.Count == 0;
        AppsListView.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        EmptyStateText.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
    }
}
