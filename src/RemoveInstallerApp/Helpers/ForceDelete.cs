using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Helpers;

/// <summary>
/// Three-step delete pipeline for a single file or folder that the normal
/// File.Delete/Directory.Delete couldn't remove: clear blocking attributes and reset
/// ownership/ACLs, and if it's still locked by another process, schedule it for deletion
/// at the next Windows startup via the PendingFileRenameOperations mechanism.
/// </summary>
public static class ForceDelete
{
    private const uint MoveFileDelayUntilReboot = 0x4;

    public static (ForceDeleteOutcome Outcome, string? Error) TryDeleteFile(string path) => TryDeleteFileCore(path);

    public static (ForceDeleteOutcome Outcome, string? Error) TryDeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return (ForceDeleteOutcome.Deleted, null);
        }

        try
        {
            Directory.Delete(path, recursive: true);
            return (ForceDeleteOutcome.Deleted, null);
        }
        catch
        {
            // Fall through to the harder per-item pipeline below.
        }

        var scheduledAny = false;
        var errors = new List<string>();

        List<string> files;
        try
        {
            files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToList();
        }
        catch (Exception ex)
        {
            return (ForceDeleteOutcome.Failed, ex.Message);
        }

        // Files first, then directories deepest-first, root last: PendingFileRenameOperations is
        // processed in this same order at boot, so a directory only needs to be empty by the time
        // its own turn comes — which holds as long as everything under it was listed earlier.
        foreach (var file in files)
        {
            var (outcome, error) = TryDeleteFileCore(file);
            switch (outcome)
            {
                case ForceDeleteOutcome.ScheduledForReboot:
                    scheduledAny = true;
                    break;
                case ForceDeleteOutcome.Failed:
                    errors.Add($"{file}: {error}");
                    break;
            }
        }

        List<string> directories;
        try
        {
            directories = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length)
                .ToList();
        }
        catch (Exception ex)
        {
            return (ForceDeleteOutcome.Failed, ex.Message);
        }
        directories.Add(path);

        foreach (var directory in directories)
        {
            if (TryDeleteEmptyDirectory(directory))
            {
                continue;
            }

            TryUnlock(directory, isDirectory: true);

            if (TryDeleteEmptyDirectory(directory))
            {
                continue;
            }

            if (ScheduleDeleteOnReboot(directory))
            {
                scheduledAny = true;
            }
            else
            {
                errors.Add($"{directory}: could not schedule for deletion on reboot (Win32 error {Marshal.GetLastWin32Error()}).");
            }
        }

        if (errors.Count > 0)
        {
            return (ForceDeleteOutcome.Failed, string.Join("; ", errors));
        }

        return scheduledAny ? (ForceDeleteOutcome.ScheduledForReboot, null) : (ForceDeleteOutcome.Deleted, null);
    }

    private static (ForceDeleteOutcome Outcome, string? Error) TryDeleteFileCore(string path)
    {
        if (!File.Exists(path))
        {
            return (ForceDeleteOutcome.Deleted, null);
        }

        try
        {
            File.Delete(path);
            return (ForceDeleteOutcome.Deleted, null);
        }
        catch
        {
            // Fall through: locked, read-only, or an ACL blocks us.
        }

        TryUnlock(path, isDirectory: false);

        try
        {
            File.Delete(path);
            return (ForceDeleteOutcome.Deleted, null);
        }
        catch
        {
            // Still failing — likely open by another process. Schedule for reboot.
        }

        if (ScheduleDeleteOnReboot(path))
        {
            return (ForceDeleteOutcome.ScheduledForReboot, null);
        }

        return (ForceDeleteOutcome.Failed, $"Could not delete or schedule for deletion (Win32 error {Marshal.GetLastWin32Error()}).");
    }

    private static bool TryDeleteEmptyDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
                return true;
            }
        }
        catch
        {
            // Not deletable yet (still has pending children, or access denied) — caller retries.
        }

        return false;
    }

    /// <summary>Clears read-only/system/hidden attributes and grants the current user full control, best-effort.</summary>
    private static void TryUnlock(string path, bool isDirectory)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & (FileAttributes.ReadOnly | FileAttributes.System | FileAttributes.Hidden)) != 0)
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
        }
        catch
        {
            // Best-effort.
        }

        try
        {
            var currentUser = WindowsIdentity.GetCurrent().User;
            if (currentUser is null)
            {
                return;
            }

            if (isDirectory)
            {
                var directoryInfo = new DirectoryInfo(path);
                var security = directoryInfo.GetAccessControl();
                security.SetOwner(currentUser);
                security.AddAccessRule(new FileSystemAccessRule(currentUser, FileSystemRights.FullControl, AccessControlType.Allow));
                directoryInfo.SetAccessControl(security);
            }
            else
            {
                var fileInfo = new FileInfo(path);
                var security = fileInfo.GetAccessControl();
                security.SetOwner(currentUser);
                security.AddAccessRule(new FileSystemAccessRule(currentUser, FileSystemRights.FullControl, AccessControlType.Allow));
                fileInfo.SetAccessControl(security);
            }
        }
        catch
        {
            // Best-effort: taking ownership can itself fail (e.g. no SeTakeOwnershipPrivilege);
            // the caller just proceeds to the reboot-scheduling fallback in that case.
        }
    }

    private static bool ScheduleDeleteOnReboot(string path) => MoveFileEx(path, null, MoveFileDelayUntilReboot);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, uint dwFlags);
}
