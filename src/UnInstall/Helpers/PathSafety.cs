namespace UnInstall.Helpers;

/// <summary>
/// Guards against ever recursively deleting a folder that isn't actually
/// specific to one app — e.g. because InstallLocation was blank, a drive
/// root, or one of the well-known system/user folders themselves.
/// </summary>
public static class PathSafety
{
    private static readonly string[] ProtectedFolders = BuildProtectedFolders();

    public static bool IsSafeToDeleteRecursively(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string full;
        try
        {
            full = Path.GetFullPath(path).TrimEnd('\\', '/');
        }
        catch
        {
            return false;
        }

        // Refuse drive roots ("C:\") and anything shorter/equal to a protected system folder.
        if (full.Length <= 3 || Path.GetPathRoot(full)?.TrimEnd('\\') == full)
        {
            return false;
        }

        foreach (var protectedFolder in ProtectedFolders)
        {
            if (string.Equals(full, protectedFolder, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Stronger guard for the Force Delete tool, which (unlike every other delete path in this
    /// app) lets the user point at an arbitrary file/folder rather than one the app itself found.
    /// Builds on <see cref="IsSafeToDeleteRecursively"/> (drive roots, exact protected folders)
    /// and additionally refuses anything anywhere under the Windows folder, not just its root —
    /// a single stray file deleted under System32 can be catastrophic. Subfolders/files inside
    /// Program Files, AppData, Desktop, etc. remain allowed: that's this tool's actual purpose
    /// (leftover app folders that won't delete normally).
    /// </summary>
    public static bool IsSafeToForceDelete(string? path)
    {
        if (!IsSafeToDeleteRecursively(path))
        {
            return false;
        }

        string full;
        try
        {
            full = Path.GetFullPath(path!).TrimEnd('\\', '/');
        }
        catch
        {
            return false;
        }

        var windowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\', '/');
        if (string.Equals(full, windowsFolder, StringComparison.OrdinalIgnoreCase) ||
            full.StartsWith(windowsFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string[] BuildProtectedFolders()
    {
        var list = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            @"C:\Users",
        };

        return list
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.TrimEnd('\\', '/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
