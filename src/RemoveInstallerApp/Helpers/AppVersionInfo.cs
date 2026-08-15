using System.Reflection;

namespace RemoveInstallerApp.Helpers;

public static class AppVersionInfo
{
    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

    public static string CurrentVersionText => CurrentVersion.ToString(3);
}
