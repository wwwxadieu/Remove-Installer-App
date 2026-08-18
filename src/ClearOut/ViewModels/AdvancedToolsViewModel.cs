using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ClearOut.Models;
using ClearOut.Services;

namespace ClearOut.ViewModels;

public sealed partial class AdvancedToolsViewModel : ObservableObject
{
    private readonly IStartupAppsService _startupAppsService;
    private readonly IWindowsServiceManager _serviceManager;
    private readonly IProcessMonitorService _processMonitorService;

    public ObservableCollection<StartupAppInfo> StartupApps { get; } = new();
    public ObservableCollection<WindowsServiceInfo> Services { get; } = new();
    public ObservableCollection<RunningProcessInfo> Processes { get; } = new();

    [ObservableProperty]
    private bool _isLoadingStartupApps;

    [ObservableProperty]
    private bool _isLoadingServices;

    [ObservableProperty]
    private bool _isLoadingProcesses;

    public AdvancedToolsViewModel(
        IStartupAppsService startupAppsService,
        IWindowsServiceManager serviceManager,
        IProcessMonitorService processMonitorService)
    {
        _startupAppsService = startupAppsService;
        _serviceManager = serviceManager;
        _processMonitorService = processMonitorService;
    }

    public async Task LoadStartupAppsAsync()
    {
        IsLoadingStartupApps = true;
        try
        {
            var apps = await _startupAppsService.GetStartupAppsAsync();
            StartupApps.Clear();
            foreach (var app in apps)
            {
                StartupApps.Add(app);
            }
        }
        finally
        {
            IsLoadingStartupApps = false;
        }
    }

    public async Task LoadServicesAsync()
    {
        IsLoadingServices = true;
        try
        {
            var services = await _serviceManager.GetServicesAsync();
            Services.Clear();
            foreach (var service in services)
            {
                Services.Add(service);
            }
        }
        finally
        {
            IsLoadingServices = false;
        }
    }

    public async Task LoadProcessesAsync()
    {
        IsLoadingProcesses = true;
        try
        {
            var processes = await _processMonitorService.GetTopProcessesByMemoryAsync();
            Processes.Clear();
            foreach (var process in processes)
            {
                Processes.Add(process);
            }
        }
        finally
        {
            IsLoadingProcesses = false;
        }
    }

    /// <summary>Updates <paramref name="app"/>.IsEnabled only on success, so a failed toggle
    /// visually reverts (the row's ToggleSwitch is OneWay-bound to IsEnabled).</summary>
    public async Task<bool> SetStartupAppEnabledAsync(StartupAppInfo app, bool enabled)
    {
        var success = await _startupAppsService.SetEnabledAsync(app, enabled);
        if (success)
        {
            app.IsEnabled = enabled;
        }
        return success;
    }

    /// <summary>Always reloads the service list afterward, whether or not the start succeeded -
    /// the service's actual status (e.g. left in StartPending) may have changed either way.</summary>
    public async Task<bool> StartServiceAsync(WindowsServiceInfo service)
    {
        var success = await _serviceManager.StartAsync(service.ServiceName);
        await LoadServicesAsync();
        return success;
    }

    public async Task<bool> StopServiceAsync(WindowsServiceInfo service)
    {
        var success = await _serviceManager.StopAsync(service.ServiceName);
        await LoadServicesAsync();
        return success;
    }

    public async Task<bool> KillProcessAsync(RunningProcessInfo process)
    {
        var success = _processMonitorService.KillProcess(process.ProcessId);
        await LoadProcessesAsync();
        return success;
    }
}
