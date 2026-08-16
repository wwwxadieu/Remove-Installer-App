using System.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ClearOut.Helpers;
using ClearOut.Models;
using ClearOut.Services;
using ClearOut.Strings;
using ClearOut.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using WinRT.Interop;

namespace ClearOut.Views;

public sealed partial class ForceDeletePage : Page
{
    public ForceDeleteViewModel ViewModel { get; }

    private readonly ILicenseService _licenseService;
    private bool _suppressSecureDeleteEvent;

    public ForceDeletePage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ForceDeleteViewModel>();
        _licenseService = App.Services.GetRequiredService<ILicenseService>();
        QueueListView.ItemsSource = ViewModel.Queue;
        ViewModel.Queue.CollectionChanged += (_, _) => UpdateEmptyState();
        SecureDeleteCheckBox.IsChecked = ViewModel.SecureDelete;

        // Windows blocks drag-and-drop from Explorer (not elevated) into this window
        // (elevated) at the OS level — UIPI, not something the app can opt out of. Telling the
        // user why up front beats a drop zone that silently does nothing and looks broken.
        if (IsRunningElevated())
        {
            ElevatedDragDropNoticeText.Visibility = Visibility.Visible;
        }

        UpdateEmptyState();
    }

    private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
    {
        var path = FileDialog.PickFile(GetWindowHandle());
        if (path is not null)
        {
            TryAddPath(path);
        }
    }

    private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var path = FileDialog.PickFolder(GetWindowHandle());
        if (path is not null)
        {
            TryAddPath(path);
        }
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.StorageItems)
            ? DataPackageOperation.Copy
            : DataPackageOperation.None;
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        foreach (var item in items)
        {
            TryAddPath(item.Path);
        }
    }

    private static bool IsRunningElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private void TryAddPath(string path)
    {
        var error = ViewModel.AddPath(path);
        if (error is not null)
        {
            AppLog.Warn($"Force Delete: could not add \"{path}\": {error}");
            _ = ShowSimpleDialogAsync(AppStrings.ForceDelete_AddErrorTitle, error);
        }
        else
        {
            UpdateEmptyState();
        }
    }

    private void RemoveQueueItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ForceDeleteQueueItem item })
        {
            ViewModel.Queue.Remove(item);
        }
    }

    /// <summary>"Delete unrecoverably" is a Pro feature — checking it without a license reverts
    /// the box and offers the upgrade dialog instead of silently enabling it.</summary>
    private async void SecureDeleteCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressSecureDeleteEvent)
        {
            return;
        }

        var wantsOn = SecureDeleteCheckBox.IsChecked == true;
        if (!wantsOn || _licenseService.IsPro)
        {
            ViewModel.SecureDelete = wantsOn;
            return;
        }

        SetSecureDeleteCheckedSuppressed(false);
        var started = await ProUpgradePrompt.ShowAsync(XamlRoot, _licenseService, AppStrings.ForceDelete_SecureDelete);
        if (started)
        {
            SetSecureDeleteCheckedSuppressed(true);
        }
        ViewModel.SecureDelete = started;
    }

    private void SetSecureDeleteCheckedSuppressed(bool value)
    {
        _suppressSecureDeleteEvent = true;
        SecureDeleteCheckBox.IsChecked = value;
        _suppressSecureDeleteEvent = false;
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedCount = ViewModel.Queue.Count(i => i.IsSelected);
        if (selectedCount == 0)
        {
            return;
        }

        var confirmDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.ForceDelete_ConfirmTitle,
            Content = ViewModel.SecureDelete
                ? AppStrings.ForceDelete_ConfirmMessageSecure(selectedCount)
                : AppStrings.ForceDelete_ConfirmMessage(selectedCount),
            PrimaryButtonText = AppStrings.Common_Yes,
            CloseButtonText = AppStrings.Common_No,
            DefaultButton = ContentDialogButton.Close,
        };

        if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        SetBusy(true, null);
        var result = await ViewModel.DeleteQueuedAsync();
        SetBusy(false, null);
        UpdateEmptyState();

        var message = AppStrings.ForceDelete_ResultSummary(result.DeletedCount, result.ScheduledForRebootCount);
        if (result.ScheduledForRebootCount > 0)
        {
            message += "\n\n" + AppStrings.ForceDelete_RebootRequiredNotice(result.ScheduledForRebootCount);
        }

        await ShowSimpleDialogAsync(AppStrings.ForceDelete_ResultTitle, message);

        if (result.Errors.Count > 0)
        {
            await ShowSimpleDialogAsync(AppStrings.ForceDelete_ErrorsTitle, string.Join("\n", result.Errors));
        }
    }

    private async Task ShowSimpleDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = AppStrings.Common_Close,
        };
        await dialog.ShowAsync();
    }

    private void SetBusy(bool isBusy, string? status)
    {
        BusyRing.IsActive = isBusy;
        StatusText.Text = status ?? string.Empty;
    }

    private void UpdateEmptyState()
    {
        var isEmpty = ViewModel.Queue.Count == 0;
        QueueListView.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        EmptyStateText.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    private static nint GetWindowHandle() => WindowNative.GetWindowHandle(App.MainAppWindow);
}
