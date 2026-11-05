// Replacement for a group of WPF DataTriggers that set one property from the value of one binding. The parameter is a
// pipe separated list of "value=result" entries and an optional default "*=result":
//   Foreground="{Binding ShopSubCategory, Converter={x:Static conv:ValueMapConverter.Instance}, ConverterParameter='sword=@SolidColorBrush.Weapon.Sword|*=@SolidColorBrush.Weapon.Unknown'}"
// The value is compared by its invariant text (enum members by name), ordinal and case sensitive like the WPF DataTrigger string
// comparison. A result that starts with @ is a resource key of the application (the Color.* / SolidColorBrush.* keys of the theme),
// anything else is parsed as the target type (string, bool, numbers, Thickness, FontWeight, brushes, enums). Without a matching entry
// and without a default the property keeps its value.
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.ComponentModel;
using System.Globalization;

namespace StatisticsAnalysisTool.Common.Converters;

public sealed class ValueMapConverter : IValueConverter
{
    public static readonly ValueMapConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string map)
        {
            return AvaloniaProperty.UnsetValue;
        }

        var text = value == null ? null : System.Convert.ToString(value, CultureInfo.InvariantCulture);
        string result = null;
        var found = false;
        string fallback = null;
        var hasFallback = false;

        foreach (var entry in map.Split('|'))
        {
            var index = entry.IndexOf('=');
            if (index < 0)
            {
                continue;
            }

            var key = entry[..index];
            if (key == "*")
            {
                fallback = entry[(index + 1)..];
                hasFallback = true;
            }
            else if (!found && text != null && string.Equals(key, text, StringComparison.Ordinal))
            {
                result = entry[(index + 1)..];
                found = true;
            }
        }

        if (!found && !hasFallback)
        {
            return AvaloniaProperty.UnsetValue;
        }

        return ToTarget(found ? result : fallback, targetType, culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();

    private static object ToTarget(string result, Type targetType, CultureInfo culture)
    {
        if (result.StartsWith('@'))
        {
            var key = result[1..];
            if (Application.Current != null && Application.Current.TryGetResource(key, Application.Current.ActualThemeVariant, out var resource))
            {
                return resource;
            }

            return BindingOperations.DoNothing;
        }

        if (targetType == typeof(object) || targetType == typeof(string))
        {
            return result;
        }

        if (targetType == typeof(IImage) || targetType == typeof(IImage))
        {
            return new Bitmap(AssetLoader.Open(new Uri(result)));
        }

        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (type == typeof(IBrush) || type == typeof(Brush))
        {
            return Brush.Parse(result);
        }

        if (type == typeof(FontWeight))
        {
            return Enum.Parse<FontWeight>(result, true);
        }

        if (type == typeof(Thickness))
        {
            return Thickness.Parse(result);
        }

        if (type == typeof(bool))
        {
            return bool.Parse(result);
        }

        return TypeDescriptor.GetConverter(type).ConvertFromString(null, CultureInfo.InvariantCulture, result);
    }
}
