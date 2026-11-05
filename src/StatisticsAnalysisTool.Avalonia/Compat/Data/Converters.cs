// Compatibility shim: System.Windows.Data converter contracts and Binding sentinels.
using System.Collections.Generic;
using System.Globalization;

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
    public static object DoNothing => Avalonia.Data.BindingOperations.DoNothing;
}

/// <summary>
/// WPF cross-thread collection synchronization hooks. Avalonia has no equivalent; the linked code always
/// mutates collections through the UI dispatcher where it matters, so these are intentionally no-ops.
/// </summary>
public static class BindingOperations
{
    public static void EnableCollectionSynchronization(System.Collections.IEnumerable collection, object lockObject)
    {
    }

    public static void EnableCollectionSynchronization(System.Collections.IEnumerable collection, object context, CollectionSynchronizationCallback synchronizationCallback)
    {
    }

    public static void DisableCollectionSynchronization(System.Collections.IEnumerable collection)
    {
    }
}

public delegate void CollectionSynchronizationCallback(System.Collections.IEnumerable collection, object context, Action accessMethod, bool writeAccess);
