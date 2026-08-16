using ClearOut.Models;

namespace ClearOut.Services;

public interface IDeviceInfoService
{
    /// <summary>
    /// Reads hardware/OS specs for the current machine (registry reads plus a few static .NET
    /// APIs) — no scanning involved, so this is synchronous and cheap enough to call straight
    /// from the UI thread on page load.
    /// </summary>
    DeviceSpecsInfo GetDeviceSpecs();
}
