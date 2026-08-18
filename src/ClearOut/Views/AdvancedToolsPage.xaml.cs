using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ClearOut.Models;
using ClearOut.Strings;
using ClearOut.ViewModels;

namespace ClearOut.Views;

/// <summary>Startup Apps / Services / running-process tools, reached from the toolbar's
/// "Advanced tools" button (not a left-nav item). Each of the three sections loads
/// independently and can be refreshed on its own.</summary>
public sealed partial class AdvancedToolsPage : Page
{
    public AdvancedToolsViewModel ViewModel { get; }

    public AdvancedToolsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<AdvancedToolsViewModel>();
        StartupAppsListView.ItemsSource = ViewModel.StartupApps;
        ServicesListView.ItemsSource = ViewModel.Services;
        ProcessesListView.ItemsSource = ViewModel.Processes;

        Loaded += async (_, _) =>
        {
            await ViewModel.LoadStartupAppsAsync();
            UpdateEmptyState();
            await ViewModel.LoadServicesAsync();
            UpdateEmptyState();
            await ViewModel.LoadProcessesAsync();
            UpdateEmptyState();
        };
    }

    private void UpdateEmptyState()
    {
        StartupAppsEmptyText.Visibility = ViewModel.StartupApps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ServicesEmptyText.Visibility = ViewModel.Services.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ProcessesEmptyText.Visibility = ViewModel.Processes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void RefreshStartupApps_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadStartupAppsAsync();
        UpdateEmptyState();
    }

    private async void RefreshServices_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadServicesAsync();
        UpdateEmptyState();
    }

    private async void RefreshProcesses_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadProcessesAsync();
        UpdateEmptyState();
    }

    private async void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch { Tag: StartupAppInfo app } toggle || toggle.IsOn == app.IsEnabled)
        {
            // Also fires when the OneWay bind re-syncs IsOn to the model after this handler
            // itself changes app.IsEnabled (or reverts it below) - not a new user action.
            return;
        }

        var desired = toggle.IsOn;
        toggle.IsEnabled = false;
        var success = await ViewModel.SetStartupAppEnabledAsync(app, desired);
        toggle.IsEnabled = true;

        if (!success)
        {
            toggle.IsOn = app.IsEnabled;
        }
    }

    private async void ServiceActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: WindowsServiceInfo service } button)
        {
            return;
        }

        button.IsEnabled = false;
        try
        {
            _ = service.IsRunning
                ? await ViewModel.StopServiceAsync(service)
                : await ViewModel.StartServiceAsync(service);
            UpdateEmptyState();
        }
        finally
        {
            // The list is reloaded above (fresh item instances), so this only matters if that
            // reload itself left the list unchanged - harmless either way.
            button.IsEnabled = true;
        }
    }

    private async void KillProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: RunningProcessInfo process })
        {
            return;
        }

        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = AppStrings.AdvancedTools_KillConfirmTitle,
            Content = AppStrings.AdvancedTools_KillConfirmMessage(process.Name),
            PrimaryButtonText = AppStrings.AdvancedTools_KillButton,
            CloseButtonText = AppStrings.Common_Cancel,
            DefaultButton = ContentDialogButton.Close,
        };

        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.KillProcessAsync(process);
        UpdateEmptyState();
    }
}
