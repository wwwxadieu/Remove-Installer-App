using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using UnInstall.Helpers;
using UnInstall.Models;
using UnInstall.Services;
using UnInstall.Strings;
using UnInstall.ViewModels;

namespace UnInstall;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static Window? MainAppWindow { get; private set; }

    /// <summary>
    /// Set when launched via the Explorer "Uninstall with UnInstall" context-menu
    /// verb: the .exe or .lnk path the user right-clicked. Null on a normal launch.
    /// </summary>
    public static string? LaunchUninstallTargetPath { get; private set; }

    /// <summary>
    /// Set when launched via the Explorer "Quick uninstall..." context-menu verb. When set, the
    /// app never creates <see cref="MainAppWindow"/> — it runs the uninstall flow with native
    /// MessageBox dialogs only and exits. Null on a normal launch.
    /// </summary>
    public static string? LaunchQuickUninstallTargetPath { get; private set; }

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();

        // Force the localization singleton into existence before any UI is built. Its
        // constructor applies the saved language, but DI creates singletons lazily — nothing
        // resolved it until the Settings page was opened, so on startup the app rendered in
        // the OS language even though the setting said otherwise.
        Services.GetRequiredService<ILocalizationService>();

        // One-time cleanup for anyone upgrading from a beta that was still named
        // "RemoveInstallerApp": if the Explorer context-menu toggle was on, its registry verbs
        // point at a .exe path that no longer exists post-rename. Cheap no-op once migrated.
        Services.GetRequiredService<IShellIntegrationService>().MigrateLegacyVerbs();

        // Without these, a XAML/UI exception kills the app with no trace, and a faulted
        // background task vanishes entirely — leaving symptoms (a tab that won't switch, a
        // frozen window) that can only be guessed at, since this app can only run on Windows.
        UnhandledException += (_, e) =>
        {
            AppLog.Error("Unhandled UI exception.", e.Exception);
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            AppLog.Error("Unobserved task exception.", e.Exception);
            e.SetObserved();
        };

        var args = Environment.GetCommandLineArgs();
        LaunchUninstallTargetPath = ParseArgument(args, "--uninstall");
        LaunchQuickUninstallTargetPath = ParseArgument(args, "--quick-uninstall");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (LaunchQuickUninstallTargetPath is { } quickUninstallPath)
        {
            _ = RunQuickUninstallAndExitAsync(quickUninstallPath);
            return;
        }

        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();
    }

    /// <summary>Consumes the pending launch target so it only triggers the uninstall flow once.</summary>
    public static void ClearLaunchUninstallTarget() => LaunchUninstallTargetPath = null;

    /// <summary>
    /// The headless counterpart of <c>AppListPage.HandlePendingLaunchTargetAsync</c> /
    /// <c>UninstallFlowAsync</c>: same confirm → (offer backup) → uninstall → result pipeline,
    /// but driven entirely by native MessageBox dialogs since there's no Window/XamlRoot here.
    /// </summary>
    private static async Task RunQuickUninstallAndExitAsync(string targetPath)
    {
        try
        {
            var resolvedPath = targetPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)
                ? ShortcutResolver.ResolveTarget(targetPath) ?? targetPath
                : targetPath;

            var installedAppsService = Services.GetRequiredService<IInstalledAppsService>();
            var apps = await installedAppsService.GetInstalledAppsAsync();
            var app = InstalledAppMatcher.FindByPath(apps, resolvedPath);

            if (app is null)
            {
                NativeMessageBox.ShowInfo(
                    AppStrings.QuickUninstall_AppNotFound(System.IO.Path.GetFileName(resolvedPath)),
                    AppStrings.QuickUninstall_ResultTitle);
                return;
            }

            if (!NativeMessageBox.Confirm(AppStrings.QuickUninstall_ConfirmUninstall(app.DisplayName), AppStrings.QuickUninstall_ResultTitle))
            {
                return;
            }

            if (NativeMessageBox.Confirm(AppStrings.QuickUninstall_ConfirmBackup(app.DisplayName), AppStrings.QuickUninstall_ResultTitle))
            {
                var backupService = Services.GetRequiredService<IBackupService>();
                var backupResult = await backupService.CreateRestorePointAsync(AppStrings.Backup_RestorePointDescription(app.DisplayName));
                if (!backupResult.Success)
                {
                    // Headless flow keeps this to a single extra dialog rather than asking
                    // "continue anyway?" again — the uninstall proceeds regardless, same as
                    // choosing "Yes" in the windowed flow's "continue anyway" prompt.
                    NativeMessageBox.ShowInfo(AppStrings.Backup_Failed(backupResult.ErrorMessage ?? string.Empty), AppStrings.QuickUninstall_ResultTitle);
                }
            }

            var orchestrator = Services.GetRequiredService<IUninstallOrchestrator>();
            var result = await orchestrator.UninstallAsync(app);

            // The headless verb has no window to host a progress UI, so it scans without one
            // and just reports the count alongside the result.
            IReadOnlyList<ResidueItem> residue = Array.Empty<ResidueItem>();
            if (result.IsSuccess)
            {
                var scanService = Services.GetRequiredService<IResidueScanService>();
                residue = await scanService.ScanAfterUninstallAsync(app);
            }

            NativeMessageBox.ShowInfo(
                UninstallResultFormatter.Format(app.DisplayName, result, residue),
                AppStrings.QuickUninstall_ResultTitle);
        }
        finally
        {
            // Application.Exit() alone can leave the process alive if some WinUI resource is
            // still holding it open (there's no window to tie the process lifetime to here);
            // Environment.Exit is the guaranteed fallback.
            Application.Current.Exit();
            Environment.Exit(0);
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var collection = new ServiceCollection();

        collection.AddSingleton<ISettingsService, SettingsService>();
        collection.AddSingleton<ILocalizationService, LocalizationService>();
        collection.AddSingleton<IInstalledAppsService, InstalledAppsService>();
        collection.AddSingleton<IUninstallService, UninstallService>();
        collection.AddSingleton<IResidueScanService, ResidueScanService>();
        collection.AddSingleton<IUpdateService, UpdateService>();
        collection.AddSingleton<IShellIntegrationService, ShellIntegrationService>();
        collection.AddSingleton<IBackupService, SystemRestoreBackupService>();
        collection.AddSingleton<IForceDeleteService, ForceDeleteService>();
        collection.AddSingleton<IUninstallOrchestrator, UninstallOrchestrator>();
        collection.AddSingleton<IDiskCleanupService, DiskCleanupService>();
        collection.AddSingleton<ILicenseService, LicenseService>();

        collection.AddTransient<AppListViewModel>();
        collection.AddTransient<ResidueScanViewModel>();
        collection.AddTransient<ForceDeleteViewModel>();
        collection.AddTransient<DiskCleanupViewModel>();
        collection.AddTransient<SettingsViewModel>();

        return collection.BuildServiceProvider();
    }

    /// <summary>Looks for "&lt;flag&gt; &lt;path&gt;" in the process's real argv (index 0 is the exe itself).</summary>
    private static string? ParseArgument(string[] args, string flag)
    {
        for (var i = 1; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
