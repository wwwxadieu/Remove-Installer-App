using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RemoveInstallerApp.Models;
using RemoveInstallerApp.Strings;
using RemoveInstallerApp.ViewModels;

namespace RemoveInstallerApp.Views;

public sealed partial class ResidueScanPage : Page
{
    public ResidueScanViewModel ViewModel { get; }

    public ResidueScanPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ResidueScanViewModel>();
        ResidueListView.ItemsSource = ViewModel.Items;
        ViewModel.Items.CollectionChanged += (_, _) => UpdateEmptyState();
        UpdateEmptyState();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is IReadOnlyList<ResidueItem> items)
        {
            ViewModel.LoadItems(items);
        }
        UpdateEmptyState();
    }

    private async void ScanOrphansButton_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, AppStrings.Residue_Scanning);
        await ViewModel.ScanForOrphansAsync();
        SetBusy(false, null);
        UpdateEmptyState();
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e) => ViewModel.SelectAll(true);

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e) => ViewModel.SelectAll(false);

    private async void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedCount = ViewModel.Items.Count(i => i.IsSelected);
        if (selectedCount == 0)
        {
            return;
        }

        var confirmDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.Residue_ConfirmDeleteTitle,
            Content = AppStrings.Residue_ConfirmDeleteMessage(selectedCount),
            PrimaryButtonText = AppStrings.Common_Yes,
            CloseButtonText = AppStrings.Common_No,
            DefaultButton = ContentDialogButton.Close,
        };

        if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        SetBusy(true, null);
        var errors = await ViewModel.DeleteSelectedAsync();
        SetBusy(false, null);
        UpdateEmptyState();

        if (errors.Count > 0)
        {
            var errorDialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = AppStrings.Residue_DeleteErrorsTitle,
                Content = string.Join("\n", errors),
                CloseButtonText = AppStrings.Common_Close,
            };
            await errorDialog.ShowAsync();
        }
    }

    private void SetBusy(bool isBusy, string? status)
    {
        BusyRing.IsActive = isBusy;
        StatusText.Text = status ?? string.Empty;
    }

    private void UpdateEmptyState()
    {
        var isEmpty = ViewModel.Items.Count == 0;
        ResidueListView.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        EmptyStateText.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
    }
}
