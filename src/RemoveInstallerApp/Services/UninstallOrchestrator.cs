using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public sealed class UninstallOrchestrator : IUninstallOrchestrator
{
    private readonly IUninstallService _uninstallService;
    private readonly ISettingsService _settingsService;

    public UninstallOrchestrator(
        IUninstallService uninstallService,
        ISettingsService settingsService)
    {
        _uninstallService = uninstallService;
        _settingsService = settingsService;
    }

    public async Task<UninstallResult> UninstallAsync(InstalledAppInfo app, CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.Current;
        var result = await _uninstallService.RunUninstallerAsync(app, settings.PreferSilentUninstall, cancellationToken);

        if (result.Outcome is UninstallOutcome.NoUninstallerFound or UninstallOutcome.UninstallerFailed)
        {
            // "Always use the app's own uninstaller" means exactly that: report what happened
            // rather than quietly deleting the install folder and registry key ourselves.
            if (settings.AlwaysUseAppUninstaller)
            {
                return result;
            }

            result = await _uninstallService.ForceRemoveAsync(app, cancellationToken);
        }

        return result;
    }
}
