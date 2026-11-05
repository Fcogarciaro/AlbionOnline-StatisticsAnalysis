// Compatibility shim: System.Windows.Media brushes backed by Avalonia brushes.
using AvaloniaBrush = Avalonia.Media.IBrush;

namespace System.Windows.Media;

public abstract class Brush : Freezable
{
    private double _opacity = 1;

    public double Opacity
    {
        get => _opacity;
        set
        {
            WritePreamble();
            _opacity = value;
        }
    }

    public abstract AvaloniaBrush ToAvalonia();
}

public sealed class SolidColorBrush : Brush
{
    private Color _color;

    public SolidColorBrush()
    {
    }

    public SolidColorBrush(Color color)
    {
        _color = color;
    }

    public Color Color
    {
        get => _color;
        set
        {
            WritePreamble();
            _color = value;
        }
    }

    public override AvaloniaBrush ToAvalonia() => new Avalonia.Media.Immutable.ImmutableSolidColorBrush(_color.ToAvalonia(), Opacity);
}

public static class Brushes
{
    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public static SolidColorBrush Transparent { get; } = Frozen(Colors.Transparent);
    public static SolidColorBrush White { get; } = Frozen(Colors.White);
    public static SolidColorBrush Black { get; } = Frozen(Colors.Black);
    public static SolidColorBrush Gray { get; } = Frozen(Colors.Gray);
    public static SolidColorBrush LightGray { get; } = Frozen(Colors.LightGray);
    public static SolidColorBrush DarkGray { get; } = Frozen(Colors.DarkGray);
    public static SolidColorBrush Red { get; } = Frozen(Colors.Red);
    public static SolidColorBrush Green { get; } = Frozen(Colors.Green);
    public static SolidColorBrush Blue { get; } = Frozen(Colors.Blue);
    public static SolidColorBrush Yellow { get; } = Frozen(Colors.Yellow);
    public static SolidColorBrush Orange { get; } = Frozen(Colors.Orange);
    public static SolidColorBrush Gold { get; } = Frozen(Colors.Gold);
}
