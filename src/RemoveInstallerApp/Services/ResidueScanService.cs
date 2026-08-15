using System.Threading;
using Microsoft.Win32;
using RemoveInstallerApp.Helpers;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public sealed class ResidueScanService : IResidueScanService
{
    private static readonly (RegistryHive Hive, RegistryView View)[] RegistryScanTargets =
    {
        (RegistryHive.LocalMachine, RegistryView.Registry64),
        (RegistryHive.LocalMachine, RegistryView.Registry32),
        (RegistryHive.CurrentUser, RegistryView.Registry64),
    };

    private static readonly string[] RunKeyPaths =
    {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
    };

    public Task<IReadOnlyList<ResidueItem>> ScanAfterUninstallAsync(InstalledAppInfo app, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var items = new List<ResidueItem>();
            var nameKey = Compact(app.DisplayName);

            ScanFolders(app, nameKey, items);
            ScanShortcuts(app.DisplayName, items);
            ScanRegistrySoftwareKeys(app, nameKey, items);
            ScanRunKeys(app.DisplayName, app.InstallLocation, items);
            ScanOrphanedUninstallEntry(app, items);

            return (IReadOnlyList<ResidueItem>)items;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<ResidueItem>> ScanOrphanedEntriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var items = new List<ResidueItem>();

            foreach (var (hive, view) in RegistryScanTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ScanOrphanedUninstallEntriesFor(hive, view, items);
                ScanOrphanedRunEntriesFor(hive, view, items);
            }

            return (IReadOnlyList<ResidueItem>)items;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<string>> DeleteAsync(IEnumerable<ResidueItem> items, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var errors = new List<string>();

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    DeleteOne(item);
                }
                catch (Exception ex)
                {
                    errors.Add($"{item.Path}: {ex.Message}");
                }
            }

            return (IReadOnlyList<string>)errors;
        }, cancellationToken);
    }

    private static void DeleteOne(ResidueItem item)
    {
        switch (item.Kind)
        {
            case ResidueKind.Folder:
                if (PathSafety.IsSafeToDeleteRecursively(item.Path) && Directory.Exists(item.Path))
                {
                    Directory.Delete(item.Path, recursive: true);
                }
                break;

            case ResidueKind.File:
            case ResidueKind.Shortcut:
                if (File.Exists(item.Path))
                {
                    File.Delete(item.Path);
                }
                break;

            case ResidueKind.RegistryKey:
            case ResidueKind.OrphanedUninstallEntry:
                if (item.Hive is { } hive && item.View is { } view)
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
                    {
                        var relativePath = StripHivePrefix(item.Path);
                        var parentPath = GetParentKeyPath(relativePath, out var leaf);
                        using var parent = baseKey.OpenSubKey(parentPath, writable: true);
                        parent?.DeleteSubKeyTree(leaf, throwOnMissingSubKey: false);
                    }
                }
                break;

            case ResidueKind.OrphanedRunEntry:
                if (item.Hive is { } runHive && item.View is { } runView && item.RegistryValueName is not null)
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(runHive, runView))
                    using (var key = baseKey.OpenSubKey(StripHivePrefix(item.Path), writable: true))
                    {
                        key?.DeleteValue(item.RegistryValueName, throwOnMissingValue: false);
                    }
                }
                break;
        }
    }

    /// <summary>Item.Path is stored with a display-friendly "HKLM\..."/"HKCU\..." prefix; registry
    /// APIs need the path relative to the already-opened hive, so the prefix must be dropped first.</summary>
    private static string StripHivePrefix(string path)
    {
        var idx = path.IndexOf('\\');
        return idx >= 0 ? path[(idx + 1)..] : path;
    }

    private static string GetParentKeyPath(string fullPath, out string leaf)
    {
        var idx = fullPath.LastIndexOf('\\');
        leaf = idx >= 0 ? fullPath[(idx + 1)..] : fullPath;
        return idx >= 0 ? fullPath[..idx] : string.Empty;
    }

    private static void ScanFolders(InstalledAppInfo app, string nameKey, List<ResidueItem> items)
    {
        if (!string.IsNullOrWhiteSpace(app.InstallLocation) && Directory.Exists(app.InstallLocation))
        {
            items.Add(MakeFolderItem(app.InstallLocation, "Install folder still present"));
        }

        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        }.Distinct().Where(Directory.Exists);

        foreach (var root in roots)
        {
            IEnumerable<string> subDirs;
            try
            {
                subDirs = Directory.EnumerateDirectories(root);
            }
            catch
            {
                continue;
            }

            foreach (var dir in subDirs)
            {
                var dirName = Path.GetFileName(dir);
                if (LooksLikeMatch(dirName, nameKey) &&
                    !string.Equals(dir, app.InstallLocation, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(MakeFolderItem(dir, $"Leftover data folder under {Path.GetFileName(root)}"));
                }
            }
        }
    }

    private static ResidueItem MakeFolderItem(string path, string description)
    {
        long? size = null;
        try
        {
            size = new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        }
        catch
        {
            // Best-effort size only; access-denied subfolders just leave the size blank.
        }

        return new ResidueItem { Kind = ResidueKind.Folder, Path = path, Description = description, SizeBytes = size };
    }

    private static void ScanShortcuts(string displayName, List<ResidueItem> items)
    {
        var folders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
        }.Distinct().Where(Directory.Exists);

        foreach (var folder in folders)
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
                if (Path.GetFileNameWithoutExtension(file).Contains(displayName, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(new ResidueItem { Kind = ResidueKind.Shortcut, Path = file, Description = "Leftover shortcut" });
                }
            }
        }
    }

    private static void ScanRegistrySoftwareKeys(InstalledAppInfo app, string nameKey, List<ResidueItem> items)
    {
        foreach (var (hive, view) in RegistryScanTargets)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var softwareKey = baseKey.OpenSubKey("SOFTWARE");
            if (softwareKey is null)
            {
                continue;
            }

            foreach (var candidate in CandidateSoftwareSubKeys(softwareKey, app.Publisher, nameKey))
            {
                items.Add(new ResidueItem
                {
                    Kind = ResidueKind.RegistryKey,
                    Path = $@"{HiveLabel(hive)}\SOFTWARE\{candidate}",
                    Description = $"Leftover registry key ({view})",
                    Hive = hive,
                    View = view,
                });
            }
        }
    }

    private static IEnumerable<string> CandidateSoftwareSubKeys(RegistryKey softwareKey, string? publisher, string nameKey)
    {
        string[] subKeyNames;
        try
        {
            subKeyNames = softwareKey.GetSubKeyNames();
        }
        catch
        {
            yield break;
        }

        foreach (var subKeyName in subKeyNames)
        {
            if (LooksLikeMatch(subKeyName, nameKey))
            {
                yield return subKeyName;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(publisher) && LooksLikeMatch(subKeyName, Compact(publisher)))
            {
                using var publisherKey = softwareKey.OpenSubKey(subKeyName);
                if (publisherKey is null)
                {
                    continue;
                }

                string[] children;
                try
                {
                    children = publisherKey.GetSubKeyNames();
                }
                catch
                {
                    continue;
                }

                foreach (var child in children.Where(c => LooksLikeMatch(c, nameKey)))
                {
                    yield return $@"{subKeyName}\{child}";
                }
            }
        }
    }

    private static void ScanRunKeys(string displayName, string? installLocation, List<ResidueItem> items)
    {
        foreach (var (hive, view) in RegistryScanTargets)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            foreach (var runPath in RunKeyPaths)
            {
                using var runKey = baseKey.OpenSubKey(runPath);
                if (runKey is null)
                {
                    continue;
                }

                foreach (var valueName in runKey.GetValueNames())
                {
                    var data = runKey.GetValue(valueName) as string ?? string.Empty;
                    var matchesName = data.Contains(displayName, StringComparison.OrdinalIgnoreCase);
                    var matchesLocation = !string.IsNullOrWhiteSpace(installLocation) &&
                                           data.Contains(installLocation, StringComparison.OrdinalIgnoreCase);

                    if (matchesName || matchesLocation)
                    {
                        items.Add(new ResidueItem
                        {
                            Kind = ResidueKind.OrphanedRunEntry,
                            Path = $@"{HiveLabel(hive)}\{runPath}",
                            Description = $"Startup entry \"{valueName}\" referencing this app",
                            Hive = hive,
                            View = view,
                            RegistryValueName = valueName,
                        });
                    }
                }
            }
        }
    }

    private static void ScanOrphanedUninstallEntry(InstalledAppInfo app, List<ResidueItem> items)
    {
        using var baseKey = RegistryKey.OpenBaseKey(app.Hive, app.View);
        using var key = baseKey.OpenSubKey(app.UninstallKeyPath);
        if (key is not null)
        {
            items.Add(new ResidueItem
            {
                Kind = ResidueKind.OrphanedUninstallEntry,
                Path = $@"{HiveLabel(app.Hive)}\{app.UninstallKeyPath}",
                Description = "Orphaned entry still listed in Add/Remove Programs",
                Hive = app.Hive,
                View = app.View,
            });
        }
    }

    private static void ScanOrphanedUninstallEntriesFor(RegistryHive hive, RegistryView view, List<ResidueItem> items)
    {
        const string uninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var uninstallKey = baseKey.OpenSubKey(uninstallPath);
        if (uninstallKey is null)
        {
            return;
        }

        foreach (var subKeyName in uninstallKey.GetSubKeyNames())
        {
            using var subKey = uninstallKey.OpenSubKey(subKeyName);
            var displayName = subKey?.GetValue("DisplayName") as string;
            var uninstallString = subKey?.GetValue("UninstallString") as string;

            if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(uninstallString))
            {
                continue;
            }

            // Only flag non-MSI entries pointing at a concrete .exe path that no longer exists;
            // MSI ("msiexec ...") entries can still uninstall correctly even without a cached exe.
            if (uninstallString.Contains("msiexec", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var exePath = ExtractExecutablePath(uninstallString);
            if (exePath is not null && !File.Exists(exePath))
            {
                items.Add(new ResidueItem
                {
                    Kind = ResidueKind.OrphanedUninstallEntry,
                    Path = $@"{HiveLabel(hive)}\{uninstallPath}\{subKeyName}",
                    Description = $"\"{displayName}\" — uninstaller no longer exists on disk",
                    Hive = hive,
                    View = view,
                });
            }
        }
    }

    private static void ScanOrphanedRunEntriesFor(RegistryHive hive, RegistryView view, List<ResidueItem> items)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        foreach (var runPath in RunKeyPaths)
        {
            using var runKey = baseKey.OpenSubKey(runPath);
            if (runKey is null)
            {
                continue;
            }

            foreach (var valueName in runKey.GetValueNames())
            {
                var data = runKey.GetValue(valueName) as string;
                var exePath = ExtractExecutablePath(data);
                if (exePath is not null && !File.Exists(exePath))
                {
                    items.Add(new ResidueItem
                    {
                        Kind = ResidueKind.OrphanedRunEntry,
                        Path = $@"{HiveLabel(hive)}\{runPath}",
                        Description = $"Startup entry \"{valueName}\" points to a missing program",
                        Hive = hive,
                        View = view,
                        RegistryValueName = valueName,
                    });
                }
            }
        }
    }

    private static string? ExtractExecutablePath(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        command = command.Trim();
        string path;
        if (command.StartsWith('"'))
        {
            var end = command.IndexOf('"', 1);
            path = end > 0 ? command[1..end] : command.Trim('"');
        }
        else
        {
            var spaceIdx = command.IndexOf(' ');
            path = spaceIdx > 0 ? command[..spaceIdx] : command;
        }

        return Path.IsPathRooted(path) ? path : null;
    }

    private static string HiveLabel(RegistryHive hive) => hive switch
    {
        RegistryHive.LocalMachine => "HKLM",
        RegistryHive.CurrentUser => "HKCU",
        _ => hive.ToString(),
    };

    private static bool LooksLikeMatch(string candidate, string compactTarget)
    {
        if (compactTarget.Length < 3)
        {
            return false;
        }

        var compactCandidate = Compact(candidate);
        return compactCandidate.Length >= 3 &&
               (compactCandidate.Contains(compactTarget) || compactTarget.Contains(compactCandidate));
    }

    private static string Compact(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
