using System.ComponentModel;
using System.Diagnostics;

namespace ClearOut.Services;

/// <summary>
/// Triggers a Windows Update scan via UsoClient.exe (the same tool Windows itself uses to drive
/// update sessions from the command line) rather than showing results in-app - a full in-app
/// update list would need a COM integration with the Windows Update Agent (WUApi), which is a
/// far bigger undertaking than this feature's value justifies.
/// </summary>
public sealed class WindowsUpdateService : IWindowsUpdateService
{
    public Task<bool> TriggerScanAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "UsoClient.exe",
                    Arguments = "StartScan",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch (Win32Exception)
            {
                // UsoClient.exe missing - some Windows Server/LTSC editions don't ship it.
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }, cancellationToken);
    }

    public void OpenWindowsUpdateSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true });
        }
        catch
        {
            // Best-effort - if Settings can't be launched there is nothing else useful to do here.
        }
    }
}
