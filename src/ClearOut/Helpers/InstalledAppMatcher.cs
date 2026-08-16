using ClearOut.Models;

namespace ClearOut.Helpers;

/// <summary>
/// Matches a file path (an .exe, or a shortcut's resolved target) against a set of installed
/// apps by install folder, UninstallString/QuietUninstallString, or DisplayIcon. Shared by the
/// windowed "Uninstall with ClearOut" context-menu verb (<c>AppListViewModel</c>)
/// and the headless "Quick uninstall" verb, which has no ViewModel to call into.
/// </summary>
public static class InstalledAppMatcher
{
    public static InstalledAppInfo? FindByPath(IEnumerable<InstalledAppInfo> apps, string filePath)
    {
        string full;
        try
        {
            full = Path.GetFullPath(filePath);
        }
        catch
        {
            return null;
        }

        var appList = apps as IReadOnlyCollection<InstalledAppInfo> ?? apps.ToList();

        var byInstallFolder = appList.FirstOrDefault(a =>
            !string.IsNullOrWhiteSpace(a.InstallLocation) &&
            full.StartsWith(a.InstallLocation!.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase));
        if (byInstallFolder is not null)
        {
            return byInstallFolder;
        }

        return appList.FirstOrDefault(a =>
            ReferencesPath(a.UninstallString, full) ||
            ReferencesPath(a.QuietUninstallString, full) ||
            ReferencesPath(a.DisplayIcon, full));
    }

    private static bool ReferencesPath(string? candidate, string fullPath) =>
        !string.IsNullOrWhiteSpace(candidate) && candidate.Contains(fullPath, StringComparison.OrdinalIgnoreCase);
}
