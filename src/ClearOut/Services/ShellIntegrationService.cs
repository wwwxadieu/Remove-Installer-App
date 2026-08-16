using Microsoft.Win32;
using ClearOut.Strings;

namespace ClearOut.Services;

/// <summary>
/// Registers a "shell" verb under HKEY_CURRENT_USER\Software\Classes so Windows Explorer shows
/// "Uninstall with ClearOut" on the right-click menu of any .exe file and any .lnk
/// shortcut (Start Menu tiles, Desktop icons, taskbar pins). Scoped to HKCU only — never writes
/// HKLM, so no other account on the machine is affected and no extra elevation is needed beyond
/// what the app already requires to run at all.
/// </summary>
public sealed class ShellIntegrationService : IShellIntegrationService
{
    private const string VerbName = "ClearOutVerb";
    private const string QuickVerbName = "ClearOutQuickVerb";

    // The app has had two earlier names ("RemoveInstallerApp", then "UnInstall"), each with its
    // own verb names pointing at that generation's .exe path. A user who enabled this toggle on
    // any older beta has one of these registered, now pointing at a path that no longer exists
    // post-rename. Left alone, that's a dead context-menu entry — see MigrateLegacyVerbs. Add a
    // new entry here on the next rename rather than replacing these.
    private static readonly (string Verb, string QuickVerb)[] LegacyVerbNames =
    {
        ("RemoveInstallerAppUninstall", "RemoveInstallerAppQuickUninstall"), // pre-1.0.0-beta13
        ("UnInstallVerb", "UnInstallQuickVerb"), // 1.0.0-beta13
    };

    private static readonly string[] TargetClasses = { "exefile", "lnkfile" };

    public bool IsRegistered
    {
        get
        {
            using var classesRoot = Registry.CurrentUser.OpenSubKey(@"Software\Classes");
            if (classesRoot is null)
            {
                return false;
            }

            return TargetClasses.All(targetClass =>
            {
                using var verbKey = classesRoot.OpenSubKey($@"{targetClass}\shell\{VerbName}");
                return verbKey is not null;
            });
        }
    }

    public void Register()
    {
        var exePath = GetExecutablePath();
        if (exePath is null)
        {
            return;
        }

        foreach (var targetClass in TargetClasses)
        {
            RegisterVerb(targetClass, VerbName, AppStrings.ContextMenu_UninstallVerb, exePath, "--uninstall");
            RegisterVerb(targetClass, QuickVerbName, AppStrings.ContextMenu_QuickUninstallVerb, exePath, "--quick-uninstall");
        }
    }

    public void Unregister()
    {
        foreach (var targetClass in TargetClasses)
        {
            using var shellKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{targetClass}\shell", writable: true);
            shellKey?.DeleteSubKeyTree(VerbName, throwOnMissingSubKey: false);
            shellKey?.DeleteSubKeyTree(QuickVerbName, throwOnMissingSubKey: false);
        }
    }

    private static void RegisterVerb(string targetClass, string verbName, string menuText, string exePath, string argument)
    {
        using var verbKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{targetClass}\shell\{verbName}");
        verbKey.SetValue(null, menuText);
        verbKey.SetValue("Icon", $"\"{exePath}\",0");

        using var commandKey = verbKey.CreateSubKey("command");
        commandKey.SetValue(null, $"\"{exePath}\" {argument} \"%1\"");
    }

    public void RefreshMenuText()
    {
        if (IsRegistered)
        {
            Register();
        }
    }

    /// <summary>
    /// One-time cleanup for users upgrading from an older-named beta: removes any old-named verb
    /// keys still registered (which now point at a .exe path that no longer exists after the
    /// rename) and, if any were found, re-registers the verbs fresh under the current name and
    /// executable path. Safe to call unconditionally on every launch — it's a no-op once no
    /// legacy keys remain.
    /// </summary>
    public void MigrateLegacyVerbs()
    {
        using var classesRoot = Registry.CurrentUser.OpenSubKey(@"Software\Classes");
        if (classesRoot is null)
        {
            return;
        }

        var foundAnyLegacy = false;

        foreach (var (legacyVerb, legacyQuickVerb) in LegacyVerbNames)
        {
            var exists = TargetClasses.Any(targetClass =>
            {
                using var verbKey = classesRoot.OpenSubKey($@"{targetClass}\shell\{legacyVerb}");
                return verbKey is not null;
            });

            if (!exists)
            {
                continue;
            }

            foundAnyLegacy = true;
            foreach (var targetClass in TargetClasses)
            {
                using var shellKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{targetClass}\shell", writable: true);
                shellKey?.DeleteSubKeyTree(legacyVerb, throwOnMissingSubKey: false);
                shellKey?.DeleteSubKeyTree(legacyQuickVerb, throwOnMissingSubKey: false);
            }
        }

        if (foundAnyLegacy)
        {
            Register();
        }
    }

    private static string? GetExecutablePath() =>
        Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
}
