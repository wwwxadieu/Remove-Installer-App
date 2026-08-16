using System.Threading;
using Microsoft.Win32;
using ClearOut.Helpers;
using ClearOut.Models;
using ClearOut.Strings;

namespace ClearOut.Services;

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

    /// <summary>
    /// A folder untouched this recently is probably still in active use by whatever created
    /// it, even if that thing doesn't currently look like an installed app (a running
    /// background service, a portable tool, a cache an app repopulates on each launch).
    /// </summary>
    private static readonly TimeSpan OrphanedFolderRecencyThreshold = TimeSpan.FromDays(90);

    /// <summary>
    /// Common vendors/platforms whose support folders should never be flagged as orphaned no
    /// matter what's currently installed — they're shared by many apps (driver stacks, browser
    /// profiles, store/package infrastructure) and matching them by name alone is exactly the
    /// kind of false positive this scan has to avoid.
    /// </summary>
    private static readonly string[] KnownVendorNameFragments =
    {
        "microsoft", "windows", "google", "mozilla", "nvidia", "intel", "amd", "adobe",
        "realtek", "dell", "lenovo", "hewlettpackard", "logitech", "steam", "epicgames",
        "packages", "windowsapps",
    };

    private readonly IInstalledAppsService _installedAppsService;

    public ResidueScanService(IInstalledAppsService installedAppsService)
    {
        _installedAppsService = installedAppsService;
    }

    public Task<IReadOnlyList<ResidueItem>> ScanAfterUninstallAsync(
        InstalledAppInfo app,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var items = new List<ResidueItem>();
            var nameKey = Compact(app.DisplayName);

            // Each entry is one visible step in the UI, so the user can follow what the scan
            // is actually doing rather than watching an unexplained spinner.
            var steps = new (string Name, Action Run)[]
            {
                (AppStrings.ScanStep_InstallFolders, () => ScanFolders(app, nameKey, items)),
                (AppStrings.ScanStep_TempFolders, () => ScanTempFolders(nameKey, items)),
                (AppStrings.ScanStep_Shortcuts, () => ScanShortcuts(app.DisplayName, items)),
                (AppStrings.ScanStep_StartupFolders, () => ScanStartupShortcuts(app.DisplayName, app.InstallLocation, items)),
                (AppStrings.ScanStep_SoftwareKeys, () => ScanRegistrySoftwareKeys(app, nameKey, items)),
                (AppStrings.ScanStep_ClassesRoot, () => ScanClassesRoot(nameKey, items)),
                (AppStrings.ScanStep_AppPaths, () => ScanAppPaths(app, items)),
                (AppStrings.ScanStep_RunKeys, () => ScanRunKeys(app.DisplayName, app.InstallLocation, items)),
                (AppStrings.ScanStep_Services, () => ScanServices(app.InstallLocation, items)),
                (AppStrings.ScanStep_ScheduledTasks, () => ScanScheduledTasks(app.InstallLocation, items)),
                (AppStrings.ScanStep_UninstallEntry, () => ScanOrphanedUninstallEntry(app, items)),
            };

            RunSteps(steps, items, progress, cancellationToken);
            return (IReadOnlyList<ResidueItem>)items;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<ResidueItem>> ScanOrphanedEntriesAsync(
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Fetched up front (and off the Task.Run below) so the folder-matching step has the
        // full installed-app list without re-scanning the registry itself mid-scan.
        var installedApps = await _installedAppsService.GetInstalledAppsAsync(cancellationToken);

        return await Task.Run(() =>
        {
            var items = new List<ResidueItem>();

            var steps = RegistryScanTargets
                .SelectMany(target => new (string Name, Action Run)[]
                {
                    ($"{AppStrings.ScanStep_UninstallEntry} ({HiveLabel(target.Hive)})",
                        () => ScanOrphanedUninstallEntriesFor(target.Hive, target.View, items)),
                    ($"{AppStrings.ScanStep_RunKeys} ({HiveLabel(target.Hive)})",
                        () => ScanOrphanedRunEntriesFor(target.Hive, target.View, items)),
                })
                .Append((AppStrings.ScanStep_OrphanedFolders, (Action)(() => ScanOrphanedFolders(installedApps, items))))
                .ToArray();

            RunSteps(steps, items, progress, cancellationToken);
            return (IReadOnlyList<ResidueItem>)items;
        }, cancellationToken);
    }

    /// <summary>
    /// Runs each scan step, reporting progress before and after. A step that throws is logged
    /// and skipped rather than aborting the whole scan — one inaccessible registry hive or
    /// protected folder should not cost the user every other result.
    /// </summary>
    private static void RunSteps(
        (string Name, Action Run)[] steps,
        List<ResidueItem> items,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < steps.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ScanProgress
            {
                StepName = steps[i].Name,
                CurrentStep = i,
                TotalSteps = steps.Length,
                ItemsFound = items.Count,
            });

            try
            {
                steps[i].Run();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                AppLog.Error($"Residue scan step \"{steps[i].Name}\" failed.", ex);
            }
        }

        progress?.Report(new ScanProgress
        {
            StepName = AppStrings.ScanStep_Done,
            CurrentStep = steps.Length,
            TotalSteps = steps.Length,
            ItemsFound = items.Count,
        });
    }

    public Task<IReadOnlyList<string>> DeleteAsync(
        IEnumerable<ResidueItem> items,
        bool permanentlyDelete,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var errors = new List<string>();
            var backupSessionId = RegistryBackup.NewSessionId();

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    DeleteOne(item, permanentlyDelete, backupSessionId);
                }
                catch (Exception ex)
                {
                    errors.Add($"{item.Path}: {ex.Message}");
                }
            }

            return (IReadOnlyList<string>)errors;
        }, cancellationToken);
    }

    private static void DeleteOne(ResidueItem item, bool permanentlyDelete, string backupSessionId)
    {
        switch (item.Kind)
        {
            case ResidueKind.Folder:
            case ResidueKind.OrphanedFolder:
                if (PathSafety.IsSafeToDeleteRecursively(item.Path) && Directory.Exists(item.Path))
                {
                    DeleteFileSystemItem(item.Path, permanentlyDelete, isFolder: true);
                }
                break;

            case ResidueKind.File:
            case ResidueKind.Shortcut:
                if (File.Exists(item.Path))
                {
                    DeleteFileSystemItem(item.Path, permanentlyDelete, isFolder: false);
                }
                break;

            case ResidueKind.ScheduledTask:
                // Deleted through schtasks rather than by removing the XML under
                // System32\Tasks: the Task Scheduler service also keeps registry state, and
                // deleting the file alone leaves a half-registered task behind.
                DeleteScheduledTask(item.Path);
                break;

            case ResidueKind.RegistryKey:
            case ResidueKind.OrphanedUninstallEntry:
            case ResidueKind.ServiceEntry:
                if (item.Hive is { } hive && item.View is { } view)
                {
                    // No Recycle Bin for the registry — the .reg export is the only undo.
                    RegistryBackup.TryExport(item.Path, backupSessionId, out _);

                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    var relativePath = StripHivePrefix(item.Path);
                    var parentPath = GetParentKeyPath(relativePath, out var leaf);

                    // HKCR entries sit directly under the hive root, so there is no parent
                    // path to open — delete straight off the base key in that case.
                    using var parent = string.IsNullOrEmpty(parentPath)
                        ? null
                        : baseKey.OpenSubKey(parentPath, writable: true);

                    (parent ?? baseKey).DeleteSubKeyTree(leaf, throwOnMissingSubKey: false);
                }
                break;

            case ResidueKind.OrphanedRunEntry:
                if (item.Hive is { } runHive && item.View is { } runView && item.RegistryValueName is not null)
                {
                    // Only a single value is removed, but exporting the parent key captures it.
                    RegistryBackup.TryExport(item.Path, backupSessionId, out _);

                    using var baseKey = RegistryKey.OpenBaseKey(runHive, runView);
                    using var key = baseKey.OpenSubKey(StripHivePrefix(item.Path), writable: true);
                    key?.DeleteValue(item.RegistryValueName, throwOnMissingValue: false);
                }
                break;
        }
    }

    /// <summary>
    /// Recycle Bin by default so a mis-detected leftover stays recoverable. If the shell
    /// refuses (no bin on that volume, item over quota) the failure is surfaced rather than
    /// silently hard-deleting — quietly turning a recoverable delete into a permanent one
    /// would defeat the point of the setting.
    /// </summary>
    private static void DeleteFileSystemItem(string path, bool permanentlyDelete, bool isFolder)
    {
        if (permanentlyDelete)
        {
            if (isFolder)
            {
                Directory.Delete(path, recursive: true);
            }
            else
            {
                File.Delete(path);
            }
            return;
        }

        if (!RecycleBin.TrySend(path, out var error))
        {
            throw new IOException($"Could not move to the Recycle Bin: {error}");
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

    /// <summary>Deletes a scheduled task by name through schtasks.exe.</summary>
    private static void DeleteScheduledTask(string taskName)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "schtasks.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        startInfo.ArgumentList.Add("/Delete");
        startInfo.ArgumentList.Add("/TN");
        startInfo.ArgumentList.Add(taskName);
        startInfo.ArgumentList.Add("/F");

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new IOException("Could not start schtasks.exe.");

        process.WaitForExit(15_000);
        if (process.ExitCode != 0)
        {
            throw new IOException($"schtasks exited with code {process.ExitCode}.");
        }
    }

    /// <summary>Leftovers an installer dropped in %TEMP% or %WINDIR%\Temp and never cleaned up.</summary>
    private static void ScanTempFolders(string nameKey, List<ResidueItem> items)
    {
        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot")
                         ?? Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        var roots = new[] { Path.GetTempPath(), Path.Combine(systemRoot, "Temp") }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(Directory.Exists);

        foreach (var root in roots)
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(root))
                {
                    if (LooksLikeMatch(Path.GetFileName(dir), nameKey))
                    {
                        items.Add(MakeFolderItem(dir, "Leftover temp folder"));
                    }
                }

                foreach (var file in Directory.EnumerateFiles(root))
                {
                    if (LooksLikeMatch(Path.GetFileNameWithoutExtension(file), nameKey))
                    {
                        items.Add(new ResidueItem
                        {
                            Kind = ResidueKind.File,
                            Path = file,
                            Description = "Leftover temp file",
                        });
                    }
                }
            }
            catch
            {
                // Temp folders routinely contain items locked by other processes.
            }
        }
    }

    /// <summary>Auto-start shortcuts the uninstaller left in the Startup folders.</summary>
    private static void ScanStartupShortcuts(string displayName, string? installLocation, List<ResidueItem> items)
    {
        var folders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
        }.Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists);

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
                var matchesName = Path.GetFileNameWithoutExtension(file)
                    .Contains(displayName, StringComparison.OrdinalIgnoreCase);

                var target = ShortcutResolver.ResolveTarget(file);
                var matchesTarget = !string.IsNullOrWhiteSpace(installLocation) &&
                                    target is not null &&
                                    target.StartsWith(installLocation!.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);

                if (matchesName || matchesTarget)
                {
                    items.Add(new ResidueItem
                    {
                        Kind = ResidueKind.Shortcut,
                        Path = file,
                        Description = "Leftover startup shortcut",
                    });
                }
            }
        }
    }

    /// <summary>App Paths entries whose registered executable no longer exists.</summary>
    private static void ScanAppPaths(InstalledAppInfo app, List<ResidueItem> items)
    {
        const string appPathsKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";

        foreach (var (hive, view) in RegistryScanTargets)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var appPaths = baseKey.OpenSubKey(appPathsKey);
            if (appPaths is null)
            {
                continue;
            }

            foreach (var subKeyName in appPaths.GetSubKeyNames())
            {
                using var subKey = appPaths.OpenSubKey(subKeyName);
                var target = subKey?.GetValue(null) as string;
                if (string.IsNullOrWhiteSpace(target))
                {
                    continue;
                }

                var exePath = target.Trim('"');
                var belongsToApp = !string.IsNullOrWhiteSpace(app.InstallLocation) &&
                                   exePath.StartsWith(app.InstallLocation!.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);

                if (belongsToApp || (Path.IsPathRooted(exePath) && !File.Exists(exePath)))
                {
                    items.Add(new ResidueItem
                    {
                        Kind = ResidueKind.RegistryKey,
                        Path = $@"{HiveLabel(hive)}\{appPathsKey}\{subKeyName}",
                        Description = $"App Paths entry pointing at \"{exePath}\"",
                        Hive = hive,
                        View = view,
                    });
                }
            }
        }
    }

    /// <summary>File-association and COM leftovers under HKEY_CLASSES_ROOT.</summary>
    private static void ScanClassesRoot(string nameKey, List<ResidueItem> items)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);

        string[] subKeyNames;
        try
        {
            subKeyNames = baseKey.GetSubKeyNames();
        }
        catch
        {
            return;
        }

        foreach (var subKeyName in subKeyNames)
        {
            // HKCR is huge and mostly extensions/CLSIDs; only flag names that clearly carry
            // the app's own name, and never bare ".ext" association keys.
            if (subKeyName.StartsWith('.') || !LooksLikeMatch(subKeyName, nameKey))
            {
                continue;
            }

            items.Add(new ResidueItem
            {
                Kind = ResidueKind.RegistryKey,
                Path = $@"HKCR\{subKeyName}",
                Description = "Leftover HKEY_CLASSES_ROOT key",
                Hive = RegistryHive.ClassesRoot,
                View = RegistryView.Registry64,
            });
        }
    }

    /// <summary>Services whose ImagePath sits inside the app's install folder.</summary>
    private static void ScanServices(string? installLocation, List<ResidueItem> items)
    {
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            return;
        }

        const string servicesKey = @"SYSTEM\CurrentControlSet\Services";
        var prefix = installLocation!.TrimEnd('\\') + "\\";

        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var services = baseKey.OpenSubKey(servicesKey);
        if (services is null)
        {
            return;
        }

        foreach (var serviceName in services.GetSubKeyNames())
        {
            using var service = services.OpenSubKey(serviceName);
            var imagePath = service?.GetValue("ImagePath") as string;
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                continue;
            }

            var exePath = ExtractExecutablePath(imagePath.Replace("\\??\\", string.Empty));
            if (exePath is null || !exePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            items.Add(new ResidueItem
            {
                Kind = ResidueKind.ServiceEntry,
                Path = $@"HKLM\{servicesKey}\{serviceName}",
                Description = $"Service \"{serviceName}\" running from the removed install folder",
                Hive = RegistryHive.LocalMachine,
                View = RegistryView.Registry64,
                // High risk: leave it to the user to opt in per item.
                IsSelected = false,
            });
        }
    }

    /// <summary>Scheduled tasks whose action points inside the app's install folder.</summary>
    private static void ScanScheduledTasks(string? installLocation, List<ResidueItem> items)
    {
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            return;
        }

        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot")
                         ?? Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var tasksRoot = Path.Combine(systemRoot, "System32", "Tasks");
        if (!Directory.Exists(tasksRoot))
        {
            return;
        }

        IEnumerable<string> taskFiles;
        try
        {
            taskFiles = Directory.EnumerateFiles(tasksRoot, "*", SearchOption.AllDirectories);
        }
        catch
        {
            return;
        }

        foreach (var taskFile in taskFiles)
        {
            string content;
            try
            {
                content = File.ReadAllText(taskFile);
            }
            catch
            {
                continue;
            }

            if (!content.Contains(installLocation!, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // schtasks identifies a task by its path relative to the Tasks root, with a
            // leading backslash — not by the file path on disk.
            var taskName = "\\" + Path.GetRelativePath(tasksRoot, taskFile).Replace('/', '\\');

            items.Add(new ResidueItem
            {
                Kind = ResidueKind.ScheduledTask,
                Path = taskName,
                Description = "Scheduled task referencing the removed install folder",
                IsSelected = false,
            });
        }
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

    /// <summary>
    /// Folders under AppData/ProgramData/Program Files that don't match any currently installed
    /// app by name, publisher, or install location. This is inference by exclusion — the
    /// riskiest kind of match in this app — so it leans hard on caution: recently touched
    /// folders and known shared-vendor folders are skipped, and every result stays unchecked
    /// (see <see cref="ResidueKindExtensions.IsHighRisk"/>) for the user to review individually.
    /// </summary>
    private static void ScanOrphanedFolders(IReadOnlyList<InstalledAppInfo> installedApps, List<ResidueItem> items)
    {
        var installedNameKeys = installedApps
            .Select(a => Compact(a.DisplayName))
            .Where(k => k.Length >= 3)
            .ToHashSet();

        var installedPublisherKeys = installedApps
            .Select(a => a.Publisher)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => Compact(p!))
            .Where(k => k.Length >= 3)
            .ToHashSet();

        var installedLocations = installedApps
            .Select(a => a.InstallLocation)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.TrimEnd('\\'))
            .ToArray();

        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        }.Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists);

        var cutoff = DateTime.Now - OrphanedFolderRecencyThreshold;

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
                if (IsExcludedFromOrphanScan(dir, installedNameKeys, installedPublisherKeys, installedLocations, cutoff))
                {
                    continue;
                }

                items.Add(new ResidueItem
                {
                    Kind = ResidueKind.OrphanedFolder,
                    Path = dir,
                    Description = AppStrings.OrphanedFolder_Description(Directory.GetLastWriteTime(dir).ToString("yyyy-MM-dd")),
                    SizeBytes = TryGetFolderSize(dir),
                    IsSelected = false,
                });
            }
        }
    }

    private static bool IsExcludedFromOrphanScan(
        string dir,
        HashSet<string> installedNameKeys,
        HashSet<string> installedPublisherKeys,
        string[] installedLocations,
        DateTime cutoff)
    {
        DateTime lastWrite;
        try
        {
            lastWrite = Directory.GetLastWriteTime(dir);
        }
        catch
        {
            return true; // Can't inspect it — don't guess.
        }

        if (lastWrite >= cutoff)
        {
            return true;
        }

        if (installedLocations.Any(loc => dir.Equals(loc, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var dirNameKey = Compact(Path.GetFileName(dir));
        if (dirNameKey.Length < 3)
        {
            // Too short to compare reliably either way — leaving it out avoids a name-collision
            // false positive more than it costs us a true one.
            return true;
        }

        if (installedNameKeys.Any(k => dirNameKey.Contains(k) || k.Contains(dirNameKey)) ||
            installedPublisherKeys.Any(k => dirNameKey.Contains(k) || k.Contains(dirNameKey)))
        {
            return true;
        }

        return KnownVendorNameFragments.Any(dirNameKey.Contains);
    }

    private static long? TryGetFolderSize(string path)
    {
        try
        {
            return new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        }
        catch
        {
            return null;
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
        RegistryHive.ClassesRoot => "HKCR",
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
