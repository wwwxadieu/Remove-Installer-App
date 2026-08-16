using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ClearOut.Helpers;
using ClearOut.Models;
using ClearOut.Services;
using ClearOut.Strings;
using ClearOut.ViewModels;

namespace ClearOut.Views;

public sealed partial class DiskCleanupPage : Page
{
    public DiskCleanupViewModel ViewModel { get; }

    private readonly ILicenseService _licenseService;

    public DiskCleanupPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DiskCleanupViewModel>();
        _licenseService = App.Services.GetRequiredService<ILicenseService>();
        CategoryListView.ItemsSource = ViewModel.Categories;
        DrivesItemsControl.ItemsSource = ViewModel.Drives;
        ViewModel.Categories.CollectionChanged += (_, _) => UpdateEmptyState();
        UpdateEmptyState();
        // Cheap (no file scanning) and drive usage changes over time, so refresh every time the
        // page is shown rather than guarding it to run once like the junk-category scan.
        Loaded += (_, _) => ViewModel.LoadDrives();
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, AppStrings.DiskCleanup_Scanning);
        ScanProgressBar.Value = 0;
        ScanProgressBar.Visibility = Visibility.Visible;

        // Progress<T> hops back to the UI thread on its own, so these assignments are safe
        // even though the scan itself runs on a background thread.
        var progress = new Progress<ScanProgress>(p =>
        {
            ScanProgressBar.Value = p.PercentComplete;
            StatusText.Text = AppStrings.ScanProgress_Status(p.StepName, p.ItemsFound);
        });

        await ViewModel.ScanAsync(progress);

        ScanProgressBar.Visibility = Visibility.Collapsed;
        SetBusy(false, null);
        UpdateEmptyState();
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e) => ViewModel.SelectAll(true);

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e) => ViewModel.SelectAll(false);

    private async void CleanButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = ViewModel.Categories.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        if (!_licenseService.IsPro)
        {
            var started = await ProUpgradePrompt.ShowAsync(XamlRoot, _licenseService, AppStrings.DiskCleanup_Clean);
            if (!started)
            {
                return;
            }
        }

        var totalBytes = selected.Sum(c => c.SizeBytes);

        var confirmDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.DiskCleanup_ConfirmTitle,
            Content = AppStrings.DiskCleanup_ConfirmMessage(FormatBytes(totalBytes), selected.Count),
            PrimaryButtonText = AppStrings.Common_Yes,
            CloseButtonText = AppStrings.Common_No,
            DefaultButton = ContentDialogButton.Close,
        };

        if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        SetBusy(true, null);
        var result = await ViewModel.CleanSelectedAsync();
        SetBusy(false, null);
        UpdateEmptyState();

        var message = AppStrings.DiskCleanup_ResultSummary(FormatBytes(result.BytesFreed));
        if (result.SkippedFileCount > 0)
        {
            message += "\n\n" + AppStrings.DiskCleanup_ResultSkipped(result.SkippedFileCount);
        }

        var resultDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.DiskCleanup_ResultTitle,
            Content = message,
            CloseButtonText = AppStrings.Common_Close,
        };
        await resultDialog.ShowAsync();

        if (result.Errors.Count > 0)
        {
            var errorDialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = AppStrings.DiskCleanup_ErrorsTitle,
                Content = string.Join("\n", result.Errors),
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
        var isEmpty = ViewModel.Categories.Count == 0;
        CategoryListView.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        EmptyStateText.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:0.#} {units[unitIndex]}";
    }
}
