using Microsoft.Win32;
using UnInstall.Strings;

namespace UnInstall.Services;

/// <summary>
/// Registers a "shell" verb under HKEY_CURRENT_USER\Software\Classes so Windows Explorer shows
/// "Uninstall with UnInstall" on the right-click menu of any .exe file and any .lnk
/// shortcut (Start Menu tiles, Desktop icons, taskbar pins). Scoped to HKCU only — never writes
/// HKLM, so no other account on the machine is affected and no extra elevation is needed beyond
/// what the app already requires to run at all.
/// </summary>
public sealed class ShellIntegrationService : IShellIntegrationService
{
    private const string VerbName = "UnInstallVerb";
    private const string QuickVerbName = "UnInstallQuickVerb";

    // The app was previously named "RemoveInstallerApp" (and its .exe had that filename), so a
    // user who enabled this toggle on an older beta has these verbs registered under the old
    // name, pointing at the old .exe path. Left alone, that's a dead context-menu entry after
    // upgrading — see MigrateLegacyVerbs.
    private const string LegacyVerbName = "RemoveInstallerAppUninstall";
    private const string LegacyQuickVerbName = "RemoveInstallerAppQuickUninstall";

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
    /// One-time cleanup for users upgrading from a beta that still called itself
    /// "RemoveInstallerApp": removes the old-named verb keys (which now point at a .exe path
    /// that no longer exists after the rename) and, if they were present, re-registers the
    /// verbs fresh under the current name and executable path. Safe to call unconditionally on
    /// every launch — it's a no-op once the legacy keys are gone.
    /// </summary>
    public void MigrateLegacyVerbs()
    {
        using var classesRoot = Registry.CurrentUser.OpenSubKey(@"Software\Classes");
        var hadLegacyVerb = classesRoot is not null && TargetClasses.Any(targetClass =>
        {
            using var verbKey = classesRoot.OpenSubKey($@"{targetClass}\shell\{LegacyVerbName}");
            return verbKey is not null;
        });

        if (!hadLegacyVerb)
        {
            return;
        }

        foreach (var targetClass in TargetClasses)
        {
            using var shellKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{targetClass}\shell", writable: true);
            shellKey?.DeleteSubKeyTree(LegacyVerbName, throwOnMissingSubKey: false);
            shellKey?.DeleteSubKeyTree(LegacyQuickVerbName, throwOnMissingSubKey: false);
        }

        Register();
    }

    private static string? GetExecutablePath() =>
        Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
}
