using ClearOut.Models;

namespace ClearOut.Services;

public interface IProcessMonitorService
{
    /// <summary>The top <paramref name="count"/> processes by working-set memory, highest first.
    /// Excludes this app's own process and PIDs 0/4 (System Idle Process / System) - neither is
    /// something a user should be able to kill from here.</summary>
    Task<IReadOnlyList<RunningProcessInfo>> GetTopProcessesByMemoryAsync(int count = 20, CancellationToken cancellationToken = default);

    /// <summary>Best-effort: false (never throws) if the process already exited, access is
    /// denied, or the PID belongs to this app or a protected system process.</summary>
    bool KillProcess(int processId);
}
