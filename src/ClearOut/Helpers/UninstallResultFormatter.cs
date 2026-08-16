using ClearOut.Models;
using ClearOut.Strings;

namespace ClearOut.Helpers;

/// <summary>
/// Turns an <see cref="UninstallResult"/> into the user-facing result message. Shared by the
/// windowed uninstall flow (<c>AppListPage</c>) and the headless "Quick uninstall" verb, so
/// both report the same outcome the same way.
///
/// Leftover counts are appended by the caller when it has them: the windowed flow scans in a
/// separate progress dialog after this message is composed, so it isn't known yet here.
/// </summary>
public static class UninstallResultFormatter
{
    public static string Format(string appName, UninstallResult result)
    {
        var message = result.Outcome switch
        {
            UninstallOutcome.UninstallerSucceeded => AppStrings.AppList_ResultSucceeded(appName),
            UninstallOutcome.ForceRemoved => AppStrings.AppList_ResultForceRemoved(appName),
            UninstallOutcome.UninstallerFailed => AppStrings.AppList_ResultFailed(appName, result.ExitCode),
            UninstallOutcome.NoUninstallerFound => AppStrings.AppList_ResultNoUninstaller(appName),
            UninstallOutcome.Error => AppStrings.AppList_ResultError(appName, result.Message ?? string.Empty),
            _ => AppStrings.AppList_ResultFailed(appName, result.ExitCode),
        };

        return message;
    }

    public static string Format(string appName, UninstallResult result, IReadOnlyList<ResidueItem> residue)
    {
        var message = Format(appName, result);

        if (residue.Count > 0)
        {
            message += "\n\n" + AppStrings.AppList_ResidueFound(residue.Count);
        }

        return message;
    }
}
