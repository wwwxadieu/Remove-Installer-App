using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;
using UnInstall.Helpers;
using UnInstall.Models;

namespace UnInstall.Services;

public sealed class UninstallService : IUninstallService
{
    public async Task<UninstallResult> RunUninstallerAsync(InstalledAppInfo app, bool silent, CancellationToken cancellationToken = default)
    {
        var command = SelectCommand(app, silent);
        if (string.IsNullOrWhiteSpace(command))
        {
            return new UninstallResult { Outcome = UninstallOutcome.NoUninstallerFound };
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{command}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new UninstallResult { Outcome = UninstallOutcome.Error, Message = "Failed to start uninstaller process." };
            }

            await process.WaitForExitAsync(cancellationToken);

            // 3010 = success, reboot required (common for MSI). Treat as success.
            var success = process.ExitCode == 0 || process.ExitCode == 3010;
            return new UninstallResult
            {
                Outcome = success ? UninstallOutcome.UninstallerSucceeded : UninstallOutcome.UninstallerFailed,
                ExitCode = process.ExitCode,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new UninstallResult { Outcome = UninstallOutcome.Error, Message = ex.Message };
        }
    }

    public Task<UninstallResult> ForceRemoveAsync(InstalledAppInfo app, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var notes = new List<string>();

            if (PathSafety.IsSafeToDeleteRecursively(app.InstallLocation))
            {
                try
                {
                    if (Directory.Exists(app.InstallLocation))
                    {
                        Directory.Delete(app.InstallLocation!, recursive: true);
                        notes.Add($"Deleted install folder: {app.InstallLocation}");
                    }
                }
                catch (Exception ex)
                {
                    notes.Add($"Could not delete install folder: {ex.Message}");
                }
            }

            var shortcutsRemoved = RemoveMatchingShortcuts(app.DisplayName);
            if (shortcutsRemoved > 0)
            {
                notes.Add($"Removed {shortcutsRemoved} shortcut(s).");
            }

            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(app.Hive, app.View);
                using var uninstallParent = baseKey.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", writable: true);
                uninstallParent?.DeleteSubKeyTree(app.RegistryKeyName, throwOnMissingSubKey: false);
                notes.Add("Removed Uninstall registry entry.");
            }
            catch (Exception ex)
            {
                notes.Add($"Could not remove Uninstall registry entry: {ex.Message}");
            }

            return new UninstallResult
            {
                Outcome = UninstallOutcome.ForceRemoved,
                Message = string.Join(" ", notes),
            };
        }, cancellationToken);
    }

    private static string? SelectCommand(InstalledAppInfo app, bool silent)
    {
        var command = silent && !string.IsNullOrWhiteSpace(app.QuietUninstallString)
            ? app.QuietUninstallString
            : app.UninstallString;

        if (string.IsNullOrWhiteSpace(command))
        {
            command = app.QuietUninstallString ?? app.UninstallString;
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        // No dedicated quiet string was published for an MSI product: msiexec itself
        // still supports silent flags, so add them ourselves when the caller asked for silent.
        if (silent &&
            string.IsNullOrWhiteSpace(app.QuietUninstallString) &&
            command.Contains("msiexec", StringComparison.OrdinalIgnoreCase) &&
            !command.Contains("/qn", StringComparison.OrdinalIgnoreCase))
        {
            command += " /qn /norestart";
        }

        return command;
    }

    private static int RemoveMatchingShortcuts(string displayName)
    {
        var removed = 0;
        var shortcutFolders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
        };

        foreach (var folder in shortcutFolders.Distinct().Where(Directory.Exists))
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(folder, "*.lnk", SearchOption.AllDirectories);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (name.Contains(displayName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Delete(file);
                        removed++;
                    }
                    catch
                    {
                        // Best-effort: skip shortcuts we can't remove (e.g. permissions).
                    }
                }
            }
        }

        return removed;
    }
}
