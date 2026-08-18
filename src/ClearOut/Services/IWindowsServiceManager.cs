using ClearOut.Models;

namespace ClearOut.Services;

public interface IWindowsServiceManager
{
    Task<IReadOnlyList<WindowsServiceInfo>> GetServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Starts a service and waits (with a timeout) for it to report Running. Returns
    /// false rather than throwing on any failure - access denied, the service doesn't support
    /// being started this way, or it times out.</summary>
    Task<bool> StartAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>Stops a service and waits (with a timeout) for it to report Stopped. Returns
    /// false rather than throwing on any failure, including the service not supporting stop.</summary>
    Task<bool> StopAsync(string serviceName, CancellationToken cancellationToken = default);
}
