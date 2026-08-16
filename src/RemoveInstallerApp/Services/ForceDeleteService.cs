using RemoveInstallerApp.Helpers;
using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public sealed class ForceDeleteService : IForceDeleteService
{
    public Task<BulkForceDeleteResult> DeleteAsync(IEnumerable<ForceDeleteQueueItem> items, bool secureDelete, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var deleted = 0;
            var scheduled = 0;
            var errors = new List<string>();

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!PathSafety.IsSafeToForceDelete(item.Path))
                {
                    errors.Add($"{item.Path}: refused to delete (protected path).");
                    continue;
                }

                try
                {
                    if (secureDelete)
                    {
                        if (item.IsFolder)
                        {
                            SecureFileWiper.OverwriteDirectoryContentsWithRandomData(item.Path);
                        }
                        else
                        {
                            SecureFileWiper.OverwriteWithRandomData(item.Path);
                        }
                    }

                    var (outcome, error) = item.IsFolder
                        ? ForceDelete.TryDeleteDirectory(item.Path)
                        : ForceDelete.TryDeleteFile(item.Path);

                    switch (outcome)
                    {
                        case ForceDeleteOutcome.Deleted:
                            deleted++;
                            break;
                        case ForceDeleteOutcome.ScheduledForReboot:
                            scheduled++;
                            break;
                        case ForceDeleteOutcome.Failed:
                            errors.Add($"{item.Path}: {error}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{item.Path}: {ex.Message}");
                }
            }

            return new BulkForceDeleteResult
            {
                DeletedCount = deleted,
                ScheduledForRebootCount = scheduled,
                Errors = errors,
            };
        }, cancellationToken);
    }
}
