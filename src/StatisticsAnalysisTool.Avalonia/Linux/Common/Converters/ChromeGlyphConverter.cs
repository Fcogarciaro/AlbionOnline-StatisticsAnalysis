// Webdings is not available on Linux. The chrome buttons keep the upstream Content ("0" minimize, "1" maximize,
// "2" restore, "r" close; the controller sets the integers 1 and 2) and this converter turns it into a line drawing.
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace StatisticsAnalysisTool.Common.Converters;

public sealed class ChromeGlyphConverter : IValueConverter
{
    public static readonly ChromeGlyphConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Convert.ToString(value, CultureInfo.InvariantCulture) switch
        {
            "0" => Geometry.Parse("M0,9 L10,9"),
            "1" => Geometry.Parse("M0,0 L10,0 L10,10 L0,10 Z"),
            "2" => Geometry.Parse("M2,0 L10,0 L10,8 M0,2 L8,2 L8,10 L0,10 Z"),
            "r" => Geometry.Parse("M0,0 L10,10 M10,0 L0,10"),
            _ => null
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
