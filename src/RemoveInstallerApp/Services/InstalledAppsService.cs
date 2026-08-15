using System.Threading;
using Microsoft.Win32;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public sealed class InstalledAppsService : IInstalledAppsService
{
    private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    /// <summary>
    /// Which (hive, view) combinations to scan. HKCU is only scanned once (Registry64) because,
    /// unlike HKLM, it is not split into a separate WOW6432Node store — scanning it twice would
    /// just enumerate the same keys again and produce duplicate entries.
    /// </summary>
    private static readonly (RegistryHive Hive, RegistryView View)[] ScanTargets =
    {
        (RegistryHive.LocalMachine, RegistryView.Registry64),
        (RegistryHive.LocalMachine, RegistryView.Registry32),
        (RegistryHive.CurrentUser, RegistryView.Registry64),
    };

    public Task<IReadOnlyList<InstalledAppInfo>> GetInstalledAppsAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var results = new List<InstalledAppInfo>();

            foreach (var (hive, view) in ScanTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CollectFrom(hive, view, results);
            }

            return (IReadOnlyList<InstalledAppInfo>)results
                .OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }, cancellationToken);
    }

    private static void CollectFrom(RegistryHive hive, RegistryView view, List<InstalledAppInfo> results)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var uninstallKey = baseKey.OpenSubKey(UninstallKeyPath);
        if (uninstallKey is null)
        {
            return;
        }

        foreach (var subKeyName in uninstallKey.GetSubKeyNames())
        {
            using var subKey = uninstallKey.OpenSubKey(subKeyName);
            if (subKey is null)
            {
                continue;
            }

            var info = TryReadEntry(subKey, subKeyName, hive, view);
            if (info is not null)
            {
                results.Add(info);
            }
        }
    }

    private static InstalledAppInfo? TryReadEntry(RegistryKey key, string keyName, RegistryHive hive, RegistryView view)
    {
        var displayName = key.GetValue("DisplayName") as string;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        // Skip Windows updates/patches (they show up under Uninstall with a ParentKeyName)
        // and hidden system components — neither is something a user meant to "uninstall an app".
        if (key.GetValue("ParentKeyName") is not null)
        {
            return null;
        }

        if (key.GetValue("SystemComponent") is int systemComponent && systemComponent == 1)
        {
            return null;
        }

        var releaseType = key.GetValue("ReleaseType") as string;
        if (releaseType is "Hotfix" or "Security Update" or "Update" or "ServicePack")
        {
            return null;
        }

        long? estimatedSizeKb = key.GetValue("EstimatedSize") is int size ? size : null;

        DateTime? installDate = null;
        if (key.GetValue("InstallDate") is string raw &&
            DateTime.TryParseExact(raw, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var parsed))
        {
            installDate = parsed;
        }

        return new InstalledAppInfo
        {
            DisplayName = displayName.Trim(),
            DisplayVersion = key.GetValue("DisplayVersion") as string,
            Publisher = key.GetValue("Publisher") as string,
            InstallLocation = key.GetValue("InstallLocation") as string,
            UninstallString = key.GetValue("UninstallString") as string,
            QuietUninstallString = key.GetValue("QuietUninstallString") as string,
            DisplayIcon = key.GetValue("DisplayIcon") as string,
            EstimatedSizeKb = estimatedSizeKb,
            InstallDate = installDate,
            RegistryKeyName = keyName,
            Hive = hive,
            View = view,
        };
    }
}
