// Compatibility shim: System.Windows.Data converter contracts and Binding sentinels.
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data;

namespace System.Windows.Data;

/// <summary>
/// Same signature as WPF; inherits the Avalonia contract so implementations are directly usable from Avalonia XAML.
/// </summary>
public interface IValueConverter : Avalonia.Data.Converters.IValueConverter
{
}

public interface IMultiValueConverter
{
    object Convert(object[] values, Type targetType, object parameter, CultureInfo culture);

    object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture);
}

public static class MultiValueConverterExtensions
{
    public static Avalonia.Data.Converters.IMultiValueConverter AsAvalonia(this IMultiValueConverter converter) =>
        new MultiValueConverterAdapter(converter);

    private sealed class MultiValueConverterAdapter(IMultiValueConverter inner) : Avalonia.Data.Converters.IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            var array = new object[values.Count];
            values.CopyTo(array, 0);
            return inner.Convert(array, targetType, parameter, culture);
        }
    }
}

public static class Binding
{
    /// <summary>Equivalent of WPF Binding.DoNothing: the binding engine ignores the converted value.</summary>
    public static object DoNothing => BindingOperations.DoNothing;
}
