using System.Management;
using ClearOut.Models;

namespace ClearOut.Services;

/// <summary>
/// Reads SMART predicted-failure status for each physical drive via WMI - there is no managed or
/// P/Invoke-free way to read SMART on Windows, so this is one of the app's few deliberate WMI uses
/// (see <see cref="SystemRestoreBackupService"/> for the others).
///
/// Win32_DiskDrive (root\cimv2, has the human-readable Model) and MSStorageDriver_FailurePredictStatus
/// (root\wmi, has the actual PredictFailure flag) are two separate classes with no clean WQL join
/// between them - PNPDeviceID/InstanceName formats differ enough between drivers (e.g. a trailing
/// "_0" on InstanceName) that a WHERE clause built from one to filter the other is easy to get
/// subtly wrong, and a wrong join fails silently: every drive would just read as Unknown with no
/// error to notice. Both tables are small (a handful of rows per machine), so both are fetched in
/// full and matched in C# instead, where the normalization is easy to see and get right.
/// </summary>
public sealed class DiskHealthService : IDiskHealthService
{
    public Task<IReadOnlyList<DiskHealthInfo>> GetDiskHealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var drives = new List<(string Model, string PnpDeviceId)>();
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM Win32_DiskDrive");
                using var results = searcher.Get();
                foreach (ManagementBaseObject result in results)
                {
                    using (result)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var model = (result["Model"] as string)?.Trim();
                        var pnpDeviceId = result["PNPDeviceID"] as string;
                        if (!string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(pnpDeviceId))
                        {
                            drives.Add((model, pnpDeviceId));
                        }
                    }
                }
            }
            catch
            {
                // No Win32_DiskDrive access at all - an empty list reads as "nothing to report".
            }

            var failurePredictions = new List<(string InstanceName, bool PredictFailure)>();
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM MSStorageDriver_FailurePredictStatus");
                using var results = searcher.Get();
                foreach (ManagementBaseObject result in results)
                {
                    using (result)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var instanceName = result["InstanceName"] as string;
                        if (!string.IsNullOrWhiteSpace(instanceName) && result["PredictFailure"] is bool predictFailure)
                        {
                            failurePredictions.Add((instanceName, predictFailure));
                        }
                    }
                }
            }
            catch
            {
                // SMART reporting unavailable (VM, some USB/NVMe drivers, older hardware) -
                // every drive below just falls back to Unknown, which is correct here.
            }

            var health = new List<DiskHealthInfo>();
            foreach (var drive in drives)
            {
                try
                {
                    var normalizedPnp = Normalize(drive.PnpDeviceId);
                    var match = failurePredictions.FirstOrDefault(f =>
                    {
                        var normalizedInstance = Normalize(f.InstanceName);
                        return normalizedInstance == normalizedPnp ||
                               normalizedInstance.StartsWith(normalizedPnp, StringComparison.Ordinal) ||
                               normalizedPnp.StartsWith(normalizedInstance, StringComparison.Ordinal);
                    });

                    var status = match.InstanceName is null
                        ? DiskHealthStatus.Unknown
                        : match.PredictFailure ? DiskHealthStatus.Warning : DiskHealthStatus.Ok;

                    health.Add(new DiskHealthInfo { DiskModel = drive.Model, Status = status });
                }
                catch
                {
                    health.Add(new DiskHealthInfo { DiskModel = drive.Model, Status = DiskHealthStatus.Unknown });
                }
            }

            return (IReadOnlyList<DiskHealthInfo>)health;
        }, cancellationToken);
    }

    /// <summary>Upper-cases and strips a trailing "_N" instance suffix (e.g. MSStorageDriver_FailurePredictStatus's
    /// InstanceName often has "_0" where Win32_DiskDrive's PNPDeviceID has none) so the two IDs compare cleanly.</summary>
    private static string Normalize(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        var underscoreIndex = normalized.LastIndexOf('_');
        if (underscoreIndex == normalized.Length - 2 && char.IsDigit(normalized[^1]))
        {
            normalized = normalized[..underscoreIndex];
        }
        return normalized;
    }
}
