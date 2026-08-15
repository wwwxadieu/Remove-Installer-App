using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RemoveInstallerApp.Strings;
using RemoveInstallerApp.Views;

namespace RemoveInstallerApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = AppStrings.AppTitle;
    }

    private void RootNavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyLocalizedLabels();
        RootNavigationView.SelectedItem = NavItemAppList;
        ContentFrame.Navigate(typeof(AppListPage));
    }

    /// <summary>Re-applied after a language switch so nav labels refresh without restarting.</summary>
    public void ApplyLocalizedLabels()
    {
        NavItemAppList.Content = AppStrings.NavInstalledApps;
        NavItemResidue.Content = AppStrings.NavLeftoverCleaner;
        if (RootNavigationView.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = AppStrings.NavSettings;
        }
        Title = AppStrings.AppTitle;
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItemContainer is NavigationViewItem { Tag: string tag })
        {
            switch (tag)
            {
                case "apps":
                    ContentFrame.Navigate(typeof(AppListPage));
                    break;
                case "residue":
                    ContentFrame.Navigate(typeof(ResidueScanPage));
                    break;
            }
        }
    }
}
