namespace ClearOut.Services;

public interface IWindowsUpdateService
{
    /// <summary>Kicks off a real Windows Update scan (via UsoClient.exe). This only triggers the
    /// scan - the result is visible in Windows Settings, not surfaced back to this app; see
    /// <see cref="OpenWindowsUpdateSettings"/>.</summary>
    Task<bool> TriggerScanAsync(CancellationToken cancellationToken = default);

    /// <summary>Opens Settings > Windows Update.</summary>
    void OpenWindowsUpdateSettings();
}
