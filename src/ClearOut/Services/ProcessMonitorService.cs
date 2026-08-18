using System.Diagnostics;
using ClearOut.Models;

namespace ClearOut.Services;

/// <summary>
/// Lists top-memory processes and can kill one, entirely through System.Diagnostics.Process
/// (no P/Invoke, no WMI). PID 0 (System Idle Process), PID 4 (System), and this app's own PID
/// are excluded from both listing and killing, so a user can't accidentally take down the OS
/// kernel process or ClearOut itself from its own process list.
/// </summary>
public sealed class ProcessMonitorService : IProcessMonitorService
{
    private static readonly HashSet<int> ProtectedProcessIds = new() { 0, 4 };

    public Task<IReadOnlyList<RunningProcessInfo>> GetTopProcessesByMemoryAsync(int count = 20, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var ownProcessId = Environment.ProcessId;
            var results = new List<RunningProcessInfo>();

            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (ProtectedProcessIds.Contains(process.Id) || process.Id == ownProcessId)
                    {
                        continue;
                    }

                    try
                    {
                        results.Add(new RunningProcessInfo
                        {
                            ProcessId = process.Id,
                            Name = process.ProcessName,
                            WorkingSetBytes = process.WorkingSet64,
                        });
                    }
                    catch
                    {
                        // Process exited between enumeration and read, or access denied
                        // (some system/elevated processes even when we're elevated) - skip it.
                    }
                }
            }

            return (IReadOnlyList<RunningProcessInfo>)results
                .OrderByDescending(p => p.WorkingSetBytes)
                .Take(count)
                .ToList();
        }, cancellationToken);
    }

    public bool KillProcess(int processId)
    {
        if (ProtectedProcessIds.Contains(processId) || processId == Environment.ProcessId)
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            process.Kill();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
