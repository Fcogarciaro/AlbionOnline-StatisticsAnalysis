// Compatibility shim: mirrors the subset of System.Windows.Application used by the linked WPF logic
// (Current.Dispatcher, Resources, TryFindResource, MainWindow, Windows, Shutdown).
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaApplication = Avalonia.Application;

namespace System.Windows;

public class Application
{
    public static Application Current { get; } = new();

    public Dispatcher Dispatcher { get; } = Dispatcher.CurrentDispatcher;

    public ResourceDictionary Resources { get; } = new();

    public Window MainWindow =>
        (AvaloniaApplication.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as Window;

    public IReadOnlyList<Avalonia.Controls.Window> Windows =>
        (AvaloniaApplication.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows
        ?? (IReadOnlyList<Avalonia.Controls.Window>) [];

    /// <summary>
    /// WPF Application.GetResourceStream: opens an embedded resource (pack:// URIs are mapped to avares://)
    /// and throws <see cref="IO.IOException"/> when it does not exist.
    /// </summary>
    public static Resources.StreamResourceInfo GetResourceStream(Uri uriResource)
    {
        ArgumentNullException.ThrowIfNull(uriResource);
        var uri = Media.Imaging.BitmapImage.MapPackUri(uriResource);
        if (!AssetLoader.Exists(uri))
        {
            throw new IO.IOException($"Cannot locate resource '{uriResource}'.");
        }

        return new Resources.StreamResourceInfo(AssetLoader.Open(uri), null);
    }

    public object TryFindResource(object resourceKey) => Resources.Lookup(resourceKey);

    public void Shutdown()
    {
        if (AvaloniaApplication.Current?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }
}

/// <summary>
/// Facade over the Avalonia application resources. Avalonia colors and solid brushes are returned as the
/// WPF-shaped <see cref="Color"/> / <see cref="SolidColorBrush"/> so linked code can cast them unchanged.
/// </summary>
public sealed class ResourceDictionary
{
    public object this[object key]
    {
        get => Lookup(key);
        set
        {
            if (AvaloniaApplication.Current is { } app && key != null)
            {
                app.Resources[key] = ToAvalonia(value);
            }
        }
    }

    public bool Contains(object key) => Lookup(key) != null;

    internal object Lookup(object key)
    {
        var app = AvaloniaApplication.Current;
        if (app == null || key == null)
        {
            return null;
        }

        return app.TryGetResource(key, app.ActualThemeVariant, out var value) ? ToShim(value) : null;
    }

    private static object ToShim(object value) => value switch
    {
        Avalonia.Media.Color color => Color.FromAvalonia(color),
        Avalonia.Media.ISolidColorBrush brush => new SolidColorBrush(Color.FromAvalonia(brush.Color)) { Opacity = brush.Opacity },
        _ => value
    };

    private static object ToAvalonia(object value) => value switch
    {
        Color color => color.ToAvalonia(),
        Brush brush => brush.ToAvalonia(),
        _ => value
    };
}
