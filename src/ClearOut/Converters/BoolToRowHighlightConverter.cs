using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ClearOut.Converters;

/// <summary>
/// Tints a list row's background when its bound "is selected" bool is true, transparent
/// otherwise. Used instead of relying on ListView's own selection visuals: AppListPage sets
/// SelectionMode="None" so its Tapped handler can implement custom toggle-on-tap + right-click
/// behavior (see AppListPage.xaml.cs), which meant nothing was drawing a selected-row highlight.
/// The "false" case still returns a real transparent SolidColorBrush (not a null Background) -
/// that's required for the whole row to stay hit-testable, per the fix in AppRow_Tapped's history.
/// </summary>
public sealed class BoolToRowHighlightConverter : IValueConverter
{
    private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not true)
        {
            return TransparentBrush;
        }

        var accent = (Color)Application.Current.Resources["SystemAccentColor"];
        return new SolidColorBrush(Color.FromArgb(48, accent.R, accent.G, accent.B));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
