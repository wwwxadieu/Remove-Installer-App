using Microsoft.UI.Xaml.Controls;
using RemoveInstallerApp.Services;
using RemoveInstallerApp.Strings;

namespace RemoveInstallerApp.Helpers;

/// <summary>
/// The "this is a Pro feature" dialog shown wherever a gated action is attempted (Force Delete's
/// secure-delete, Disk Cleanup's clean action). Shared so every gate point looks and behaves the
/// same, and offers the same one-click local trial start.
/// </summary>
public static class ProUpgradePrompt
{
    /// <summary>Shows the upgrade dialog. Returns true if the user started the trial, so the caller can proceed.</summary>
    public static async Task<bool> ShowAsync(XamlRoot xamlRoot, ILicenseService licenseService, string featureName)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = AppStrings.License_UpgradeTitle,
            Content = AppStrings.License_UpgradeMessage(featureName),
            PrimaryButtonText = AppStrings.License_StartTrialButton,
            CloseButtonText = AppStrings.Common_Close,
            DefaultButton = ContentDialogButton.Primary,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return false;
        }

        licenseService.StartTrial();
        return true;
    }
}
