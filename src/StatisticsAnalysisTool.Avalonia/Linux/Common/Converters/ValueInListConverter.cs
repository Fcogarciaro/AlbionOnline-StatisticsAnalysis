// Several DataTriggers with the same setters (one per city name, "Thetford", "Thetford Portal", "Bank of Thetford") become one
// class: Classes.thetford="{Binding Location, Converter={x:Static conv:ValueInListConverter.Instance}, ConverterParameter='A|B|C'}".
// The value is compared by its invariant text, ordinal and case sensitive like the WPF DataTrigger string comparison.
using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.Linq;

namespace StatisticsAnalysisTool.Common.Converters;

public sealed class ValueInListConverter : IValueConverter
{
    public static readonly ValueInListConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter is not string list)
        {
            return false;
        }

        var text = System.Convert.ToString(value, CultureInfo.InvariantCulture);
        return list.Split('|').Contains(text, StringComparer.Ordinal);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
