using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UnInstall.Helpers;
using UnInstall.Models;
using UnInstall.Services;
using UnInstall.Strings;

namespace UnInstall.ViewModels;

public sealed partial class ForceDeleteViewModel : ObservableObject
{
    private readonly IForceDeleteService _forceDeleteService;

    public ObservableCollection<ForceDeleteQueueItem> Queue { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _secureDelete;

    public ForceDeleteViewModel(IForceDeleteService forceDeleteService)
    {
        _forceDeleteService = forceDeleteService;
    }

    /// <summary>Validates and queues a path. Returns null on success, or a user-facing error message.</summary>
    public string? AddPath(string path)
    {
        if (Queue.Any(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var isFolder = Directory.Exists(path);
        var isFile = !isFolder && File.Exists(path);
        if (!isFolder && !isFile)
        {
            return AppStrings.ForceDelete_PathNotFound(path);
        }

        if (!PathSafety.IsSafeToForceDelete(path))
        {
            return AppStrings.ForceDelete_UnsafePath(path);
        }

        long? size = null;
        try
        {
            size = isFolder
                ? new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length)
                : new FileInfo(path).Length;
        }
        catch
        {
            // Best-effort size only.
        }

        Queue.Add(new ForceDeleteQueueItem { Path = path, IsFolder = isFolder, SizeBytes = size });
        return null;
    }

    public void RemoveSelected()
    {
        foreach (var item in Queue.Where(i => i.IsSelected).ToList())
        {
            Queue.Remove(item);
        }
    }

    public async Task<BulkForceDeleteResult> DeleteQueuedAsync()
    {
        var selected = Queue.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return new BulkForceDeleteResult();
        }

        IsBusy = true;
        try
        {
            var result = await _forceDeleteService.DeleteAsync(selected, SecureDelete);
            var failedPaths = result.Errors.Select(e => e.Split(':')[0]).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in selected.Where(i => !failedPaths.Contains(i.Path)))
            {
                Queue.Remove(item);
            }

            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
