using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ClearOut.Helpers;
using ClearOut.Models;
using ClearOut.Services;
using ClearOut.Strings;

namespace ClearOut.Views;

/// <summary>
/// Shown once the app's own uninstaller has exited: scans for leftovers with visible
/// step-by-step progress, then lets the user review and delete what was found.
///
/// This is a dialog rather than a page on purpose. Navigating the main Frame to the leftover
/// cleaner left the NavigationView still highlighting "Installed apps", so clicking that tab
/// raised no SelectionChanged and appeared to do nothing — the user had to detour via another
/// tab to get back. A dialog sits above the shell and leaves navigation state untouched.
/// </summary>
public sealed partial class PostUninstallDialog : ContentDialog
{
    private readonly IResidueScanService _residueScanService;
    private readonly ISettingsService _settingsService;
    private readonly InstalledAppInfo _app;
    private readonly string _resultMessage;

    private ObservableCollection<ResidueItem> Items { get; } = new();

    public PostUninstallDialog(InstalledAppInfo app, string resultMessage)
    {
        InitializeComponent();

        _app = app;
        _resultMessage = resultMessage;
        _residueScanService = App.Services.GetRequiredService<IResidueScanService>();
        _settingsService = App.Services.GetRequiredService<ISettingsService>();

        Title = AppStrings.PostUninstall_Title;
        ResultText.Text = resultMessage;
        ResidueListView.ItemsSource = Items;

        // Scan phase: no actions offered until it finishes.
        CloseButtonText = AppStrings.Common_Close;
        IsPrimaryButtonEnabled = false;

        Opened += async (_, _) => await RunScanAsync();
    }

    private async Task RunScanAsync()
    {
        ScanStatusText.Text = AppStrings.ScanProgress_Status(AppStrings.ScanStep_InstallFolders, 0);

        var progress = new Progress<ScanProgress>(p =>
        {
            ScanProgressBar.Value = p.PercentComplete;
            ScanStatusText.Text = AppStrings.ScanProgress_Status(p.StepName, p.ItemsFound);
        });

        IReadOnlyList<ResidueItem> found;
        try
        {
            found = await _residueScanService.ScanAfterUninstallAsync(_app, progress);
        }
        catch (Exception ex)
        {
            AppLog.Error($"Post-uninstall scan failed for {_app.DisplayName}.", ex);
            ScanStatusText.Text = AppStrings.PostUninstall_ScanFailed;
            return;
        }

        foreach (var item in found)
        {
            Items.Add(item);
        }

        ShowResultsPhase();
    }

    private void ShowResultsPhase()
    {
        ScanPanel.Visibility = Visibility.Collapsed;
        ResultsPanel.Visibility = Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        ResultText.Text = Items.Count > 0
            ? _resultMessage + "\n\n" + AppStrings.AppList_ResidueFound(Items.Count)
            : _resultMessage + "\n\n" + AppStrings.PostUninstall_NothingFound;

        if (Items.Count > 0)
        {
            PrimaryButtonText = AppStrings.Residue_DeleteSelected;
            IsPrimaryButtonEnabled = true;
            // Deletion is destructive, so Close stays the default action on Enter.
            DefaultButton = ContentDialogButton.Close;
            PrimaryButtonClick += OnDeleteSelectedAsync;
        }
    }

    private async void OnDeleteSelectedAsync(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var selected = Items.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0)
        {
            args.Cancel = true;
            return;
        }

        // Keep the dialog open so the outcome can be reported in place.
        args.Cancel = true;
        var deferral = args.GetDeferral();

        try
        {
            IsPrimaryButtonEnabled = false;
            ScanPanel.Visibility = Visibility.Visible;
            ScanProgressBar.IsIndeterminate = true;
            ScanStatusText.Text = AppStrings.PostUninstall_Deleting;

            var errors = await _residueScanService.DeleteAsync(selected, _settingsService.Current.PermanentlyDelete);

            var failedPaths = errors.Select(e => e.Split(':')[0]).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var item in selected.Where(i => !failedPaths.Contains(i.Path)))
            {
                Items.Remove(item);
            }

            ScanProgressBar.IsIndeterminate = false;
            ScanPanel.Visibility = Visibility.Collapsed;

            var deletedCount = selected.Count - errors.Count;
            ResultText.Text = _settingsService.Current.PermanentlyDelete
                ? AppStrings.PostUninstall_DeletedPermanently(deletedCount)
                : AppStrings.PostUninstall_DeletedToRecycleBin(deletedCount);

            if (errors.Count > 0)
            {
                ResultText.Text += "\n\n" + AppStrings.Residue_DeleteErrorsTitle + ":\n" + string.Join("\n", errors);
            }

            ResultsPanel.Visibility = Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            IsPrimaryButtonEnabled = Items.Count > 0;
        }
        catch (Exception ex)
        {
            AppLog.Error("Deleting leftovers from the post-uninstall dialog failed.", ex);
            ScanProgressBar.IsIndeterminate = false;
            ScanPanel.Visibility = Visibility.Collapsed;
            ResultText.Text = ex.Message;
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e) => SetAllSelected(true);

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e) => SetAllSelected(false);

    private void SetAllSelected(bool selected)
    {
        foreach (var item in Items)
        {
            item.IsSelected = selected;
        }
    }
}
