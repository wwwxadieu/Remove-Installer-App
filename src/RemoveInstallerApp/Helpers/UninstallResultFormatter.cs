using RemoveInstallerApp.Models;
using RemoveInstallerApp.Strings;

namespace RemoveInstallerApp.Helpers;

/// <summary>
/// Turns an <see cref="UninstallResult"/> (plus any leftover residue found afterward) into the
/// user-facing result message. Shared by the windowed uninstall flow (<c>AppListPage</c>) and the
/// headless "Quick uninstall" verb, so both report the same outcome the same way.
/// </summary>
public static class UninstallResultFormatter
{
    public static string Format(string appName, UninstallResult result, IReadOnlyList<ResidueItem> residue)
    {
        var message = result.Outcome switch
        {
            UninstallOutcome.UninstallerSucceeded => AppStrings.AppList_ResultSucceeded(appName),
            UninstallOutcome.ForceRemoved => AppStrings.AppList_ResultForceRemoved(appName),
            UninstallOutcome.UninstallerFailed => AppStrings.AppList_ResultFailed(appName, result.ExitCode),
            UninstallOutcome.Error => AppStrings.AppList_ResultError(appName, result.Message ?? string.Empty),
            _ => AppStrings.AppList_ResultFailed(appName, result.ExitCode),
        };

        if (residue.Count > 0)
        {
            message += "\n\n" + AppStrings.AppList_ResidueFound(residue.Count);
        }

        return message;
    }
}
