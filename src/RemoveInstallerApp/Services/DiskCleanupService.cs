using RemoveInstallerApp.Helpers;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

/// <summary>
/// Scans and cleans a fixed set of OS-wide temp/cache locations, mirroring Windows' own Disk
/// Cleanup tool. Unlike the app's other delete paths, these targets are fixed well-known
/// system folders rather than user-supplied paths, so there's no PathSafety check here — the
/// safety boundary is simply "only ever touch the contents of these specific folders."
/// </summary>
public sealed class DiskCleanupService : IDiskCleanupService
{
    public Task<IReadOnlyList<DiskCleanupCategory>> ScanAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var categories = new List<DiskCleanupCategory>();

            foreach (var kind in Enum.GetValues<DiskCleanupCategoryKind>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var size = kind == DiskCleanupCategoryKind.RecycleBin
                    ? RecycleBinInterop.GetSizeBytes()
                    : GetFolderTargets(kind).Sum(t => GetSizeBytes(t));

                categories.Add(new DiskCleanupCategory { Kind = kind, SizeBytes = size });
            }

            return (IReadOnlyList<DiskCleanupCategory>)categories;
        }, cancellationToken);
    }

    public Task<DiskCleanupResult> CleanAsync(IEnumerable<DiskCleanupCategory> categories, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            long bytesFreed = 0;
            var skipped = 0;
            var errors = new List<string>();

            foreach (var category in categories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (category.Kind == DiskCleanupCategoryKind.RecycleBin)
                {
                    var sizeBeforeEmpty = RecycleBinInterop.GetSizeBytes();
                    if (RecycleBinInterop.Empty())
                    {
                        bytesFreed += sizeBeforeEmpty;
                    }
                    else
                    {
                        errors.Add($"{category.DisplayName}: could not empty the Recycle Bin.");
                    }
                    continue;
                }

                foreach (var target in GetFolderTargets(category.Kind))
                {
                    var (freed, skippedInTarget) = CleanTarget(target);
                    bytesFreed += freed;
                    skipped += skippedInTarget;
                }
            }

            return new DiskCleanupResult { BytesFreed = bytesFreed, SkippedFileCount = skipped, Errors = errors };
        }, cancellationToken);
    }

    private static (long BytesFreed, int Skipped) CleanTarget(FolderTarget target)
    {
        if (!Directory.Exists(target.DirectoryPath))
        {
            return (0, 0);
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(target.DirectoryPath, target.SearchPattern,
                target.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }
        catch
        {
            return (0, 0);
        }

        long freed = 0;
        var skipped = 0;

        foreach (var file in files)
        {
            long length;
            try
            {
                length = new FileInfo(file).Length;
                File.Delete(file);
                freed += length;
            }
            catch
            {
                // Expected and common: temp/cache files are frequently locked by another
                // process. Best-effort sweep — count it and move on.
                skipped++;
            }
        }

        if (target.Recursive)
        {
            RemoveEmptySubdirectories(target.DirectoryPath);
        }

        return (freed, skipped);
    }

    private static void RemoveEmptySubdirectories(string rootPath)
    {
        List<string> subDirectories;
        try
        {
            subDirectories = Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length)
                .ToList();
        }
        catch
        {
            return;
        }

        foreach (var directory in subDirectories)
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch
            {
                // Best-effort: leave directories we can't remove (e.g. still in use).
            }
        }
    }

    private static long GetSizeBytes(FolderTarget target)
    {
        if (!Directory.Exists(target.DirectoryPath))
        {
            return 0;
        }

        try
        {
            return Directory.EnumerateFiles(target.DirectoryPath, target.SearchPattern,
                    target.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Sum(f => new FileInfo(f).Length);
        }
        catch
        {
            return 0;
        }
    }

    private static IReadOnlyList<FolderTarget> GetFolderTargets(DiskCleanupCategoryKind kind)
    {
        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var explorerCacheFolder = Path.Combine(localAppData, "Microsoft", "Windows", "Explorer");

        return kind switch
        {
            DiskCleanupCategoryKind.TemporaryFiles => new[]
            {
                new FolderTarget(Path.GetTempPath(), "*", true),
                new FolderTarget(Path.Combine(systemRoot, "Temp"), "*", true),
            },
            DiskCleanupCategoryKind.ThumbnailCache => new[]
            {
                new FolderTarget(explorerCacheFolder, "thumbcache_*.db", false),
                new FolderTarget(explorerCacheFolder, "iconcache_*.db", false),
            },
            DiskCleanupCategoryKind.WindowsUpdateCleanup => new[]
            {
                new FolderTarget(Path.Combine(systemRoot, "SoftwareDistribution", "Download"), "*", true),
            },
            DiskCleanupCategoryKind.DeliveryOptimizationFiles => new[]
            {
                new FolderTarget(
                    Path.Combine(systemRoot, "ServiceProfiles", "NetworkService", "AppData", "Local", "Microsoft", "Windows", "DeliveryOptimization", "Cache"),
                    "*", true),
            },
            DiskCleanupCategoryKind.WindowsErrorReports => new[]
            {
                new FolderTarget(Path.Combine(programData, "Microsoft", "Windows", "WER", "ReportArchive"), "*", true),
                new FolderTarget(Path.Combine(programData, "Microsoft", "Windows", "WER", "ReportQueue"), "*", true),
            },
            DiskCleanupCategoryKind.MemoryDumpFiles => new[]
            {
                new FolderTarget(Path.Combine(systemRoot, "Minidump"), "*.dmp", false),
                new FolderTarget(systemRoot, "MEMORY.DMP", false),
            },
            _ => Array.Empty<FolderTarget>(),
        };
    }

    private sealed record FolderTarget(string DirectoryPath, string SearchPattern, bool Recursive);
}
