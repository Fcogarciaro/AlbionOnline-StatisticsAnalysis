// Converts the resource path strings the upstream XAML keeps in Tag/bindings ("pack://application:,,,/Assets/x.png" or
// an avares URI) into a bitmap, because Avalonia does not convert strings to image sources inside bindings.
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Concurrent;
using System.Globalization;

namespace StatisticsAnalysisTool.Common.Converters;

public sealed class ImagePathToBitmapConverter : IValueConverter
{
    private const string PackPrefix = "pack://application:,,,";

    public static readonly ImagePathToBitmapConverter Instance = new();

    private static readonly ConcurrentDictionary<string, Bitmap> Cache = new(StringComparer.Ordinal);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var uriText = path.StartsWith(PackPrefix, StringComparison.OrdinalIgnoreCase)
            ? "avares://StatisticsAnalysisTool" + path[PackPrefix.Length..]
            : path;

        if (Cache.TryGetValue(uriText, out var cached))
        {
            return cached;
        }

        try
        {
            var uri = new Uri(uriText, UriKind.Absolute);
            if (!AssetLoader.Exists(uri))
            {
                return null;
            }

            using var stream = AssetLoader.Open(uri);
            return Cache[uriText] = new Bitmap(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
