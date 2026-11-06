// Two-way converter between the shim System.Windows.GridLength used by view models (e.g. the saved splitter position) and
// Avalonia's GridLength, which RowDefinition.Height / ColumnDefinition.Width require. Anything else converts to "*".
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace StatisticsAnalysisTool.Common.Converters;

public sealed class GridLengthConverter : IValueConverter
{
    public static readonly GridLengthConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is System.Windows.GridLength shim
            ? shim.ToAvalonia()
            : new global::Avalonia.Controls.GridLength(1, global::Avalonia.Controls.GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is global::Avalonia.Controls.GridLength length
            ? System.Windows.GridLength.FromAvalonia(length)
            : new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
    }
}
