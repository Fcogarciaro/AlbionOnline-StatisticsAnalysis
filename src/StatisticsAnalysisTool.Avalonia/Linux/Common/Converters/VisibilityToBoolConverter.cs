// Visibility-returning bindings do not exist in Avalonia (IsVisible is a bool). View models that expose the shim
// System.Windows.Visibility are bound as
//   IsVisible="{Binding SomeVisibility, Converter={x:Static conv:VisibilityToBoolConverter.Instance}}"
// with xmlns:conv="clr-namespace:StatisticsAnalysisTool.Common.Converters". Use Inverted where WPF bound Collapsed to "true".
// Booleans pass through, so the same converter also replaces BooleanToVisibilityConverter.
using System;
using System.Globalization;
using System.Windows;
using Avalonia.Data.Converters;

namespace StatisticsAnalysisTool.Common.Converters;

public sealed class VisibilityToBoolConverter : IValueConverter
{
    public static readonly VisibilityToBoolConverter Instance = new(false);

    public static readonly VisibilityToBoolConverter Inverted = new(true);

    private readonly bool _invert;

    private VisibilityToBoolConverter(bool invert)
    {
        _invert = invert;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visible = value switch
        {
            Visibility visibility => visibility == Visibility.Visible,
            bool boolean => boolean,
            _ => false
        };

        return visible ^ _invert;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolean && (boolean ^ _invert) ? Visibility.Visible : Visibility.Collapsed;
    }
}
