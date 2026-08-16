using System.Runtime.InteropServices;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

/// <summary>
/// Creates a Windows System Restore point via Srclient.dll!SRSetRestorePointW — the OS's own
/// "backup before installing/uninstalling software" mechanism, so an uninstall gone wrong (files
/// or registry) can be rolled back from rstrui.exe without this app needing its own backup format.
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
