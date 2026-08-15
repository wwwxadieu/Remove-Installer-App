using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using RemoveInstallerApp.Services;
using RemoveInstallerApp.ViewModels;

namespace RemoveInstallerApp;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static Window? MainAppWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();
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

        collection.AddTransient<AppListViewModel>();
        collection.AddTransient<ResidueScanViewModel>();
        collection.AddTransient<SettingsViewModel>();

        return collection.BuildServiceProvider();
    }
}
