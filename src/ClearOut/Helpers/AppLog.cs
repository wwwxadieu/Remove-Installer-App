using System.Text;

namespace ClearOut.Helpers;

/// <summary>
/// Appends diagnostics to %LOCALAPPDATA%\RemoveInstallerApp\error.log — the same folder
/// SettingsService already uses (see the comment there on why that folder name stays as-is
/// after the app's rename). This exists because the app can only be built and run on
/// Windows: without an on-disk record, a runtime failure on a user's machine (a page
/// constructor throwing, a navigation silently doing nothing) is completely invisible and can
/// only be guessed at.
///
/// Every method is best-effort and must never throw: logging a problem must not itself become
/// one, and these are called from exception handlers and navigation code where a secondary
/// failure would mask the original.
/// </summary>
public static class AppLog
{
    private const long MaxLogBytes = 512 * 1024;

    private static readonly object WriteLock = new();

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RemoveInstallerApp",
        "error.log");

    public static string LogFilePath => LogPath;

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string context, Exception? ex)
    {
        var details = ex is null
            ? "(no exception object)"
            : $"{ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}";
        Write("ERROR", $"{context}\n{details}");
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (WriteLock)
            {
                var dir = Path.GetDirectoryName(LogPath)!;
                Directory.CreateDirectory(dir);
                TrimIfTooLarge();

                var line = new StringBuilder()
                    .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                    .Append(" [").Append(level).Append("] ")
                    .AppendLine(message)
                    .ToString();

                File.AppendAllText(LogPath, line, Encoding.UTF8);
            }
        }
        catch
        {
            // Deliberately swallowed — see the class remarks. A failure to log must never
            // surface as a failure of the operation being logged.
        }
    }

    /// <summary>Keeps the log from growing without bound on a long-lived install.</summary>
    private static void TrimIfTooLarge()
    {
        try
        {
            var info = new FileInfo(LogPath);
            if (info.Exists && info.Length > MaxLogBytes)
            {
                File.Delete(LogPath);
            }
        }
        catch
        {
            // Best-effort.
        }
    }
}
