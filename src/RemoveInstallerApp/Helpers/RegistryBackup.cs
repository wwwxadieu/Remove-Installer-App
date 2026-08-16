using System.Diagnostics;

namespace RemoveInstallerApp.Helpers;

/// <summary>
/// Exports a registry key to a .reg file before it is deleted. The registry has no Recycle
/// Bin, so this is the only way a mistaken registry cleanup can be undone — double-clicking
/// the exported file puts the key back.
///
/// Backups live under %LOCALAPPDATA%\RemoveInstallerApp\RegistryBackups\&lt;timestamp&gt;\,
/// grouped per cleanup run so one session's changes can be reverted together.
/// </summary>
public static class RegistryBackup
{
    private static readonly string BackupRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RemoveInstallerApp",
        "RegistryBackups");

    /// <summary>Folder name for one cleanup run; pass the same value for every key in that run.</summary>
    public static string NewSessionId() => DateTime.Now.ToString("yyyyMMdd-HHmmss");

    /// <summary>
    /// Exports "HKLM\Some\Key" (or HKCU) to a .reg file. Best-effort: a failure is logged and
    /// reported, never thrown — losing a backup must not block the cleanup the user asked for,
    /// but it must not pass silently either.
    /// </summary>
    public static bool TryExport(string displayKeyPath, string sessionId, out string? error)
    {
        error = null;

        try
        {
            var sessionDir = Path.Combine(BackupRoot, sessionId);
            Directory.CreateDirectory(sessionDir);

            var fileName = MakeSafeFileName(displayKeyPath) + ".reg";
            var filePath = Path.Combine(sessionDir, fileName);

            var startInfo = new ProcessStartInfo
            {
                FileName = "reg.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            startInfo.ArgumentList.Add("export");
            startInfo.ArgumentList.Add(displayKeyPath);
            startInfo.ArgumentList.Add(filePath);
            startInfo.ArgumentList.Add("/y");

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                error = "Could not start reg.exe.";
                AppLog.Warn($"Registry backup failed for {displayKeyPath}: {error}");
                return false;
            }

            process.WaitForExit(15_000);

            if (process.ExitCode != 0)
            {
                error = $"reg.exe exited with code {process.ExitCode}.";
                AppLog.Warn($"Registry backup failed for {displayKeyPath}: {error}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            AppLog.Error($"Registry backup threw for {displayKeyPath}.", ex);
            return false;
        }
    }

    private static string MakeSafeFileName(string keyPath)
    {
        var cleaned = keyPath.Replace('\\', '_');
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            cleaned = cleaned.Replace(invalid, '_');
        }

        // Keep well clear of MAX_PATH once the session folder is prepended.
        return cleaned.Length > 120 ? cleaned[..120] : cleaned;
    }
}
