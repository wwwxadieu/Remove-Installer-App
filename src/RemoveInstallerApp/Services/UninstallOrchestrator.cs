using RemoveInstallerApp.Models;

namespace RemoveInstallerApp.Services;

public sealed class UninstallOrchestrator : IUninstallOrchestrator
{
    private readonly IUninstallService _uninstallService;
    private readonly IResidueScanService _residueScanService;
    private readonly ISettingsService _settingsService;

    public UninstallOrchestrator(
        IUninstallService uninstallService,
        IResidueScanService residueScanService,
        ISettingsService settingsService)
    {
        _uninstallService = uninstallService;
        _residueScanService = residueScanService;
        _settingsService = settingsService;
    }

    public async Task<(UninstallResult Result, IReadOnlyList<ResidueItem> Residue)> UninstallAsync(InstalledAppInfo app, CancellationToken cancellationToken = default)
    {
        var result = await _uninstallService.RunUninstallerAsync(app, _settingsService.Current.PreferSilentUninstall, cancellationToken);

        if (result.Outcome is UninstallOutcome.NoUninstallerFound or UninstallOutcome.UninstallerFailed)
        {
            result = await _uninstallService.ForceRemoveAsync(app, cancellationToken);
        }

        IReadOnlyList<ResidueItem> residue = Array.Empty<ResidueItem>();
        if (result.IsSuccess)
        {
            residue = await _residueScanService.ScanAfterUninstallAsync(app, cancellationToken);
        }

        return (result, residue);
    }
}
