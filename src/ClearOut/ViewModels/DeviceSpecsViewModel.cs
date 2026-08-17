using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ClearOut.Models;
using ClearOut.Services;

namespace ClearOut.ViewModels;

public sealed partial class DeviceSpecsViewModel : ObservableObject
{
    private readonly IDeviceInfoService _deviceInfoService;
    private readonly IDiskCleanupService _diskCleanupService;

    public ObservableCollection<DriveSpaceInfo> Drives { get; } = new();

    [ObservableProperty]
    private DeviceSpecsInfo? _specs;

    public DeviceSpecsViewModel(IDeviceInfoService deviceInfoService, IDiskCleanupService diskCleanupService)
    {
        _deviceInfoService = deviceInfoService;
        _diskCleanupService = diskCleanupService;
    }

    /// <summary>Cheap (registry reads + DriveInfo.GetDrives(), no scanning), so it's safe to
    /// call straight from the page's Loaded handler and refresh every time the page is shown.</summary>
    public void Load()
    {
        Specs = _deviceInfoService.GetDeviceSpecs();

        Drives.Clear();
        foreach (var drive in _diskCleanupService.GetDriveSpaceInfo())
        {
            Drives.Add(drive);
        }
    }
}
