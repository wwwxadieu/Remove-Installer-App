using Microsoft.Win32;
using ClearOut.Models;

namespace ClearOut.Services;

/// <summary>
/// Startup Apps management through the registry only - the same mechanism Task Manager's Startup
/// tab uses (no P/Invoke, no WMI). Entries come from two places: the Run key (HKLM/HKCU x
/// 64/32-bit view, mirroring InstalledAppsService's Hive x View scan) and the Startup shell
/// folders (per-user + all-users). Enabled/disabled state for either kind is a binary flag under
/// "...\Explorer\StartupApproved\Run" or "...\StartupApproved\StartupFolder" (same hive/view as
/// the entry itself) - byte[0] is 0x02 (disabled) or 0x06 (enabled); a missing approval value
/// means "never toggled", i.e. enabled. Setting it only flips that flag - it never deletes the
/// underlying Run value or touches the shortcut file, so disabling is always reversible and never
/// breaks the entry it came from.
/// </summary>
public sealed class StartupAppsService : IStartupAppsService
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupApprovedRunPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string StartupApprovedFolderPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";
    private const byte EnabledFlag = 0x06;
    private const byte DisabledFlag = 0x02;

    private static readonly (RegistryHive Hive, RegistryView View)[] RunKeyScanTargets =
    {
        (RegistryHive.LocalMachine, RegistryView.Registry64),
        (RegistryHive.LocalMachine, RegistryView.Registry32),
        (RegistryHive.CurrentUser, RegistryView.Registry64),
    };

    public Task<IReadOnlyList<StartupAppInfo>> GetStartupAppsAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var results = new List<StartupAppInfo>();

            foreach (var (hive, view) in RunKeyScanTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CollectRunKeyEntries(hive, view, results);
            }

            CollectStartupFolderEntries(RegistryHive.CurrentUser, Environment.GetFolderPath(Environment.SpecialFolder.Startup), results);
            CollectStartupFolderEntries(RegistryHive.LocalMachine, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), results);

            return (IReadOnlyList<StartupAppInfo>)results
                .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }, cancellationToken);
    }

    public Task<bool> SetEnabledAsync(StartupAppInfo app, bool enabled, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                var approvedPath = app.Location == StartupAppLocation.RunKey ? StartupApprovedRunPath : StartupApprovedFolderPath;
                using var baseKey = RegistryKey.OpenBaseKey(app.Hive, app.View);
                using var approvedKey = baseKey.CreateSubKey(approvedPath, writable: true);
                if (approvedKey is null)
                {
                    return false;
                }

                var existing = approvedKey.GetValue(app.ApprovalKeyName) as byte[];
                var flagBytes = existing is { Length: > 0 } ? (byte[])existing.Clone() : new byte[12];
                flagBytes[0] = enabled ? EnabledFlag : DisabledFlag;

                approvedKey.SetValue(app.ApprovalKeyName, flagBytes, RegistryValueKind.Binary);
                return true;
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    private static void CollectRunKeyEntries(RegistryHive hive, RegistryView view, List<StartupAppInfo> results)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var runKey = baseKey.OpenSubKey(RunKeyPath);
        if (runKey is null)
        {
            return;
        }

        using var approvedKey = baseKey.OpenSubKey(StartupApprovedRunPath);

        foreach (var valueName in runKey.GetValueNames())
        {
            if (string.IsNullOrWhiteSpace(valueName))
            {
                continue;
            }

            results.Add(new StartupAppInfo
            {
                Name = valueName,
                Command = runKey.GetValue(valueName) as string,
                Location = StartupAppLocation.RunKey,
                Hive = hive,
                View = view,
                ApprovalKeyName = valueName,
                IsEnabled = IsApproved(approvedKey, valueName),
            });
        }
    }

    private static void CollectStartupFolderEntries(RegistryHive hive, string folderPath, List<StartupAppInfo> results)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            return;
        }

        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var approvedKey = baseKey.OpenSubKey(StartupApprovedFolderPath);

        foreach (var filePath in Directory.EnumerateFiles(folderPath))
        {
            var fileName = Path.GetFileName(filePath);
            if (string.Equals(fileName, "desktop.ini", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(new StartupAppInfo
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                Command = filePath,
                Location = StartupAppLocation.StartupFolder,
                Hive = hive,
                View = RegistryView.Registry64,
                ApprovalKeyName = fileName,
                IsEnabled = IsApproved(approvedKey, fileName),
            });
        }
    }

    private static bool IsApproved(RegistryKey? approvedKey, string valueName)
    {
        // No entry at all means it was never toggled through Task Manager/this app - default enabled.
        if (approvedKey?.GetValue(valueName) is not byte[] { Length: > 0 } flagBytes)
        {
            return true;
        }

        return flagBytes[0] == EnabledFlag;
    }
}
