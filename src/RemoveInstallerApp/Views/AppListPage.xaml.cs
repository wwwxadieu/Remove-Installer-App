using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RemoveInstallerApp.Models;
using RemoveInstallerApp.Strings;
using RemoveInstallerApp.ViewModels;

namespace RemoveInstallerApp.Views;

public sealed partial class AppListPage : Page
{
    public AppListViewModel ViewModel { get; }

    public AppListPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<AppListViewModel>();
        AppsListView.ItemsSource = ViewModel.Apps;
        ViewModel.Apps.CollectionChanged += (_, _) => UpdateEmptyState();
        Loaded += async (_, _) => await RefreshAsync();
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

    private async void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: InstalledAppInfo app })
        {
            return;
        }

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

        SetBusy(true, AppStrings.AppList_Loading);
        var (result, residue) = await ViewModel.UninstallAppAsync(app);
        SetBusy(false, null);
        UpdateEmptyState();

        var message = result.Outcome switch
        {
            UninstallOutcome.UninstallerSucceeded => AppStrings.AppList_ResultSucceeded(app.DisplayName),
            UninstallOutcome.ForceRemoved => AppStrings.AppList_ResultForceRemoved(app.DisplayName),
            UninstallOutcome.UninstallerFailed => AppStrings.AppList_ResultFailed(app.DisplayName, result.ExitCode),
            UninstallOutcome.Error => AppStrings.AppList_ResultError(app.DisplayName, result.Message ?? string.Empty),
            _ => AppStrings.AppList_ResultFailed(app.DisplayName, result.ExitCode),
        };

        if (residue.Count > 0)
        {
            message += "\n\n" + AppStrings.AppList_ResidueFound(residue.Count);
        }

        var resultDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.AppList_ResultTitle,
            Content = message,
            PrimaryButtonText = residue.Count > 0 ? AppStrings.AppList_GoToResidue : null,
            CloseButtonText = AppStrings.Common_Close,
        };

        if (await resultDialog.ShowAsync() == ContentDialogResult.Primary && residue.Count > 0)
        {
            Frame.Navigate(typeof(ResidueScanPage), residue);
        }
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
