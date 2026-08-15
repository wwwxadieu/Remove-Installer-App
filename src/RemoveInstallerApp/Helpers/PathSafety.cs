namespace RemoveInstallerApp.Helpers;

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
