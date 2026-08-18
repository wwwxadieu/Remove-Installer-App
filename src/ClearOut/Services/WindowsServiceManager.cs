using System.ServiceProcess;
using Microsoft.Win32;
using ClearOut.Models;

namespace ClearOut.Services;

/// <summary>
/// Lists and controls Windows services via <see cref="ServiceController"/> - the fully-managed
/// BCL API for this, not WMI/P-Invoke. ServiceController doesn't expose a service's configured
/// start type (Automatic/Manual/Disabled), so that one piece is read separately from the registry
/// (HKLM\SYSTEM\CurrentControlSet\Services\{name}\Start).
/// </summary>
public sealed class WindowsServiceManager : IWindowsServiceManager
{
    private const string ServicesKeyPath = @"SYSTEM\CurrentControlSet\Services";
    private static readonly TimeSpan StatusChangeTimeout = TimeSpan.FromSeconds(15);

    public Task<IReadOnlyList<WindowsServiceInfo>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var results = new List<WindowsServiceInfo>();

            foreach (var controller in ServiceController.GetServices())
            {
                using (controller)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        results.Add(new WindowsServiceInfo
                        {
                            ServiceName = controller.ServiceName,
                            DisplayName = controller.DisplayName,
                            Status = controller.Status,
                            StartType = ReadStartType(controller.ServiceName),
                            CanStop = controller.CanStop,
                        });
                    }
                    catch
                    {
                        // A service that can't be fully read (permissions, mid-uninstall) is
                        // skipped rather than shown with blank/wrong data.
                    }
                }
            }

            return (IReadOnlyList<WindowsServiceInfo>)results
                .OrderBy(s => s.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }, cancellationToken);
    }

    public Task<bool> StartAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var controller = new ServiceController(serviceName);
                controller.Start();
                controller.WaitForStatus(ServiceControllerStatus.Running, StatusChangeTimeout);
                return controller.Status == ServiceControllerStatus.Running;
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    public Task<bool> StopAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var controller = new ServiceController(serviceName);
                controller.Stop();
                controller.WaitForStatus(ServiceControllerStatus.Stopped, StatusChangeTimeout);
                return controller.Status == ServiceControllerStatus.Stopped;
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    private static ServiceStartType ReadStartType(string serviceName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var serviceKey = baseKey.OpenSubKey($@"{ServicesKeyPath}\{serviceName}");
            if (serviceKey?.GetValue("Start") is not int startValue)
            {
                return ServiceStartType.Other;
            }

            return startValue switch
            {
                2 => ServiceStartType.Automatic,
                3 => ServiceStartType.Manual,
                4 => ServiceStartType.Disabled,
                _ => ServiceStartType.Other, // 0 (Boot) / 1 (System) - kernel/driver-level, not user-toggleable
            };
        }
        catch
        {
            return ServiceStartType.Other;
        }
    }
}
