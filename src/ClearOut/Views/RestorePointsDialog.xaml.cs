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
/// Lets the user create a System Restore point on demand, and browse/restore existing ones.
/// Restoring reboots the machine to finish (the same as rstrui.exe), so restoring goes through
/// an in-dialog confirm phase with an explicit warning before the WMI call fires.
/// </summary>
public sealed partial class RestorePointsDialog : ContentDialog
{
    private readonly IBackupService _backupService;
    private ObservableCollection<RestorePointInfo> Points { get; } = new();
    private RestorePointInfo? _pendingPoint;

    public RestorePointsDialog()
    {
        InitializeComponent();

        _backupService = App.Services.GetRequiredService<IBackupService>();
        Title = AppStrings.RestorePoints_Title;
        PointsListView.ItemsSource = Points;
        PrimaryButtonClick += OnPrimaryButtonClickAsync;

        Opened += async (_, _) => await LoadPointsAsync();
    }

    private async Task LoadPointsAsync()
    {
        SetBusy(true);
        try
        {
            var points = await _backupService.GetRestorePointsAsync();
            Points.Clear();
            foreach (var point in points)
            {
                Points.Add(point);
            }

            EmptyStateText.Visibility = Points.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        CreateButton.IsEnabled = false;
        SetBusy(true);
        try
        {
            var result = await _backupService.CreateRestorePointAsync(AppStrings.RestorePoints_ManualDescription);
            StatusText.Text = result.Success
                ? AppStrings.RestorePoints_CreateSucceeded
                : AppStrings.RestorePoints_CreateFailed(result.ErrorMessage ?? string.Empty);

            if (result.Success)
            {
                await LoadPointsAsync();
            }
        }
        finally
        {
            CreateButton.IsEnabled = true;
            SetBusy(false);
        }
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: RestorePointInfo point })
        {
            return;
        }

        _pendingPoint = point;
        StatusText.Text = string.Empty;
        ListPanel.Visibility = Visibility.Collapsed;
        ConfirmPanel.Visibility = Visibility.Visible;
        ConfirmText.Text = AppStrings.RestorePoints_ConfirmWarning(point.Description);
        PrimaryButtonText = AppStrings.RestorePoints_ConfirmButton;
        IsPrimaryButtonEnabled = true;
        DefaultButton = ContentDialogButton.Close;
    }

    private void BackToListButton_Click(object sender, RoutedEventArgs e) => ReturnToListPhase();

    private void ReturnToListPhase()
    {
        _pendingPoint = null;
        ConfirmPanel.Visibility = Visibility.Collapsed;
        ListPanel.Visibility = Visibility.Visible;
        PrimaryButtonText = string.Empty;
        IsPrimaryButtonEnabled = false;
    }

    private async void OnPrimaryButtonClickAsync(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_pendingPoint is null)
        {
            return;
        }

        // Keep the dialog open so the outcome can be reported in place; a successful restore
        // reboots the machine on its own, outside anything this app does from here.
        args.Cancel = true;
        var deferral = args.GetDeferral();
        var point = _pendingPoint;

        try
        {
            IsPrimaryButtonEnabled = false;
            SetBusy(true);

            var success = await _backupService.RestoreToPointAsync(point.SequenceNumber);

            if (success)
            {
                ConfirmText.Text = AppStrings.RestorePoints_RestoreSucceeded;
            }
            else
            {
                AppLog.Warn($"RestoreToPointAsync failed for sequence {point.SequenceNumber}.");
                StatusText.Text = AppStrings.RestorePoints_RestoreFailed;
                ReturnToListPhase();
            }
        }
        finally
        {
            SetBusy(false);
            deferral.Complete();
        }
    }

    private void SetBusy(bool isBusy) => BusyRing.IsActive = isBusy;
}
