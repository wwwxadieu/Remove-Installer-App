using System.Management;
using System.Runtime.InteropServices;
using ClearOut.Models;

namespace ClearOut.Services;

/// <summary>
/// Creates a Windows System Restore point via Srclient.dll!SRSetRestorePointW — the OS's own
/// "backup before installing/uninstalling software" mechanism, so an uninstall gone wrong (files
/// or registry) can be rolled back from rstrui.exe without this app needing its own backup format.
///
/// Listing and restoring points has no P/Invoke or fully-managed .NET equivalent, so those two
/// operations go through the WMI SystemRestore class instead (System.Management) - the one
/// deliberate exception to this app's "managed APIs only, no WMI" rule, scoped narrowly to what
/// genuinely has no other way to do it.
/// </summary>
public sealed class SystemRestoreBackupService : IBackupService
{
    private const int BeginSystemChange = 100;
    private const int EndSystemChange = 101;
    private const int ApplicationUninstall = 2;
    private const int MaxDescriptionLength = 255;

    public Task<BackupResult> CreateRestorePointAsync(string description, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                var beginInfo = new RESTOREPOINTINFO
                {
                    dwEventType = BeginSystemChange,
                    dwRestorePtType = ApplicationUninstall,
                    llSequenceNumber = 0,
                    szDescription = Truncate(description),
                };

                if (!SRSetRestorePointW(ref beginInfo, out var beginStatus))
                {
                    return new BackupResult { Success = false, ErrorMessage = $"SRSetRestorePointW failed (Win32 error {Marshal.GetLastWin32Error()})." };
                }

                var endInfo = new RESTOREPOINTINFO
                {
                    dwEventType = EndSystemChange,
                    dwRestorePtType = ApplicationUninstall,
                    llSequenceNumber = beginStatus.llSequenceNumber,
                    szDescription = Truncate(description),
                };

                // Best-effort close of the change session; the restore point from BEGIN_SYSTEM_CHANGE
                // already exists at this point even if this second call fails.
                SRSetRestorePointW(ref endInfo, out _);

                return new BackupResult { Success = true };
            }
            catch (DllNotFoundException)
            {
                return new BackupResult { Success = false, ErrorMessage = "System Restore is not available on this machine." };
            }
            catch (EntryPointNotFoundException)
            {
                return new BackupResult { Success = false, ErrorMessage = "System Restore is not available on this machine." };
            }
            catch (Exception ex)
            {
                return new BackupResult { Success = false, ErrorMessage = ex.Message };
            }
        }, cancellationToken);
    }

    public Task<IReadOnlyList<RestorePointInfo>> GetRestorePointsAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var points = new List<RestorePointInfo>();

            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\default", "SELECT * FROM SystemRestore");
                using var results = searcher.Get();

                foreach (ManagementBaseObject result in results)
                {
                    using (result)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var creationTimeRaw = result["CreationTime"] as string;
                        points.Add(new RestorePointInfo
                        {
                            SequenceNumber = (uint)result["SequenceNumber"],
                            Description = result["Description"] as string ?? string.Empty,
                            CreationTime = creationTimeRaw is null
                                ? DateTime.MinValue
                                : ManagementDateTimeConverter.ToDateTime(creationTimeRaw),
                        });
                    }
                }
            }
            catch
            {
                // WMI unavailable, System Restore disabled, or the SystemRestore class missing
                // entirely (some Windows editions strip it) - an empty list reads correctly as
                // "nothing to restore to" either way.
            }

            return (IReadOnlyList<RestorePointInfo>)points
                .OrderByDescending(p => p.SequenceNumber)
                .ToList();
        }, cancellationToken);
    }

    public Task<bool> RestoreToPointAsync(uint sequenceNumber, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var restoreClass = new ManagementClass(@"root\default:SystemRestore");
                using var inParams = restoreClass.GetMethodParameters("Restore");
                inParams["SequenceNumber"] = sequenceNumber;

                using var outParams = restoreClass.InvokeMethod("Restore", inParams, null);
                var returnValue = outParams?["ReturnValue"] is uint code ? code : 1u;
                return returnValue == 0;
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    private static string Truncate(string value) =>
        value.Length > MaxDescriptionLength ? value[..MaxDescriptionLength] : value;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RESTOREPOINTINFO
    {
        public int dwEventType;
        public int dwRestorePtType;
        public long llSequenceNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STATEMGRSTATUS
    {
        public int nStatus;
        public long llSequenceNumber;
    }

    [DllImport("Srclient.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SRSetRestorePointW(ref RESTOREPOINTINFO restorePointInfo, out STATEMGRSTATUS status);
}
