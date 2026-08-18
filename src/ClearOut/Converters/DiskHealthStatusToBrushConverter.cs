using ClearOut.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ClearOut.Converters;

/// <summary>Maps a DiskHealthStatus to the themed fill brush used for its status pill in
/// DiskHealthDialog - Ok/Warning/Unknown reuse WinUI 3's built-in success/caution/neutral fill
/// brushes rather than hardcoding colors that would need separate light/dark values.</summary>
public sealed class DiskHealthStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var resourceKey = value switch
        {
            DiskHealthStatus.Ok => "SystemFillColorSuccessBrush",
            DiskHealthStatus.Warning => "SystemFillColorCautionBrush",
            _ => "SystemFillColorNeutralBrush",
        };

        return Application.Current.Resources[resourceKey];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
