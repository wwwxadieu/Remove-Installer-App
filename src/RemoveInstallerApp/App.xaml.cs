using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using RemoveInstallerApp.Services;
using RemoveInstallerApp.ViewModels;

namespace RemoveInstallerApp;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static Window? MainAppWindow { get; private set; }

    /// <summary>
    /// Set when launched via the Explorer "Uninstall with Remove Installer App" context-menu
    /// verb: the .exe or .lnk path the user right-clicked. Null on a normal launch.
    /// </summary>
    public static string? LaunchUninstallTargetPath { get; private set; }

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
        LaunchUninstallTargetPath = ParseUninstallTargetArgument(Environment.GetCommandLineArgs());
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();
    }

    /// <summary>Consumes the pending launch target so it only triggers the uninstall flow once.</summary>
    public static void ClearLaunchUninstallTarget() => LaunchUninstallTargetPath = null;

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

        collection.AddTransient<AppListViewModel>();
        collection.AddTransient<ResidueScanViewModel>();
        collection.AddTransient<SettingsViewModel>();

        return collection.BuildServiceProvider();
    }

    /// <summary>Looks for "--uninstall &lt;path&gt;" in the process's real argv (index 0 is the exe itself).</summary>
    private static string? ParseUninstallTargetArgument(string[] args)
    {
        for (var i = 1; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--uninstall", StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
