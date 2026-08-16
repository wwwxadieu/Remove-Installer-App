using System.Reflection;

namespace UnInstall.Helpers;

public static class AppVersionInfo
{
    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

    public static string CurrentVersionText => CurrentVersion.ToString(3);

    /// <summary>
    /// The full semver text (e.g. "1.0.0-beta5") from AssemblyInformationalVersion, which MSBuild
    /// populates from &lt;Version&gt;/-p:Version — unlike <see cref="CurrentVersion"/>'s numeric-only
    /// System.Version, this preserves prerelease suffixes, so it changes on every beta build even
    /// though the assembly's numeric version stays "1.0.0" across all of them.
    /// </summary>
    public static string CurrentInformationalVersionText =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? CurrentVersionText;
}
