using Microsoft.UI.Xaml;
using ClearOut.Models;

namespace ClearOut.Helpers;

/// <summary>
/// Applies the light/dark preference to the live window. WinUI has no app-wide runtime theme
/// switch — <c>Application.RequestedTheme</c> can only be set before the first window exists —
/// so the theme is driven from the window's root element instead, which does update instantly.
/// </summary>
public static class ThemeHelper
{
    public static void Apply(ThemeMode mode)
    {
        if (App.MainAppWindow?.Content is not FrameworkElement root)
        {
            return;
        }

        root.RequestedTheme = mode switch
        {
            ThemeMode.Light => ElementTheme.Light,
            ThemeMode.Dark => ElementTheme.Dark,
            // Default (not Light) is what actually follows the Windows setting.
            _ => ElementTheme.Default,
        };
    }
}
