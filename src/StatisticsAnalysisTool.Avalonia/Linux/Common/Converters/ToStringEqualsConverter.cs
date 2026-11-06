// DataTrigger Value="1" on an int: XAML cannot pass an int as ConverterParameter, so the value and the parameter are compared
// by their invariant text (ColorIndex 1 equals the parameter "1"). Use with Classes.name="{Binding X, Converter=..., ConverterParameter=1}".
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace StatisticsAnalysisTool.Common.Converters;

public sealed class ToStringEqualsConverter : IValueConverter
{
    public static readonly ToStringEqualsConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.Equals(System.Convert.ToString(value, CultureInfo.InvariantCulture), System.Convert.ToString(parameter, CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
