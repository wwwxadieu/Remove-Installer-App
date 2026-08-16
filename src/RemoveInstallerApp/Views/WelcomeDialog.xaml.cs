using Microsoft.UI.Xaml.Controls;
using RemoveInstallerApp.Strings;

namespace RemoveInstallerApp.Views;

/// <summary>
/// Shown once on first launch (as an app introduction) and again after every version change
/// (as a "what's new" changelog, sourced from that version's GitHub release notes). Which mode
/// it renders is decided by the caller — see <c>MainWindow.ShowWelcomeOrWhatsNewIfNeededAsync</c>.
/// </summary>
public sealed partial class WelcomeDialog : ContentDialog
{
    public WelcomeDialog()
    {
        InitializeComponent();
        // Set in code rather than via x:Bind on the XAML root element: every other x:Bind in
        // this app targets a child element, and binding the root's own properties is a
        // different (riskier) code path — not worth it for a one-off static string.
        PrimaryButtonText = AppStrings.Common_OK;
    }

    public void ConfigureAsWelcome()
    {
        Title = AppStrings.Welcome_Title;
        BodyText.Text = AppStrings.Welcome_Intro + "\n\n" + AppStrings.Welcome_FeatureList;
    }

    public void ConfigureAsWhatsNew(string version, string? releaseNotes)
    {
        Title = AppStrings.Welcome_UpdatedTitle(version);
        BodyText.Text = string.IsNullOrWhiteSpace(releaseNotes)
            ? AppStrings.Welcome_UpdatedGenericMessage
            : releaseNotes;
    }
}
