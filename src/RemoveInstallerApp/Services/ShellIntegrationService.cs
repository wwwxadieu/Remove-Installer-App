using Microsoft.Win32;
using RemoveInstallerApp.Strings;

namespace RemoveInstallerApp.Services;

/// <summary>
/// Registers a "shell" verb under HKEY_CURRENT_USER\Software\Classes so Windows Explorer shows
/// "Uninstall with Remove Installer App" on the right-click menu of any .exe file and any .lnk
/// shortcut (Start Menu tiles, Desktop icons, taskbar pins). Scoped to HKCU only — never writes
/// HKLM, so no other account on the machine is affected and no extra elevation is needed beyond
/// what the app already requires to run at all.
/// </summary>
public sealed class ShellIntegrationService : IShellIntegrationService
{
    private const string VerbName = "RemoveInstallerAppUninstall";
    private const string QuickVerbName = "RemoveInstallerAppQuickUninstall";
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

    private static string? GetExecutablePath() =>
        Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
}
