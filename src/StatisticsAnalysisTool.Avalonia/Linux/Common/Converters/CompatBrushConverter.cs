// View models written for WPF expose System.Windows.Media brushes (the Compat shim). Avalonia bindings cannot convert them to
// IBrush, so a binding to such a property uses this converter: Foreground="{Binding TitleBrush, Converter={x:Static conv:CompatBrushConverter.Instance}}".
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace StatisticsAnalysisTool.Common.Converters;

public sealed class CompatBrushConverter : IValueConverter
{
    public static readonly CompatBrushConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is System.Windows.Media.Brush brush ? brush.ToAvalonia() : value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
