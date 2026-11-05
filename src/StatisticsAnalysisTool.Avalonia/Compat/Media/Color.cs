// Compatibility shim: System.Windows.Media.Color / Colors backed by Avalonia.Media.Color.
using AvaloniaColor = Avalonia.Media.Color;

namespace System.Windows.Media;

public readonly struct Color : IEquatable<Color>
{
    public Color(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public byte A { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    public static Color FromRgb(byte r, byte g, byte b) => new(255, r, g, b);

    public static Color FromArgb(byte a, byte r, byte g, byte b) => new(a, r, g, b);

    public static Color FromAvalonia(AvaloniaColor color) => new(color.A, color.R, color.G, color.B);

    public AvaloniaColor ToAvalonia() => AvaloniaColor.FromArgb(A, R, G, B);

    public static implicit operator AvaloniaColor(Color color) => color.ToAvalonia();

    public static implicit operator Color(AvaloniaColor color) => FromAvalonia(color);

    public static bool operator ==(Color left, Color right) => left.Equals(right);

    public static bool operator !=(Color left, Color right) => !left.Equals(right);

    public bool Equals(Color other) => A == other.A && R == other.R && G == other.G && B == other.B;

    public override bool Equals(object obj) => obj is Color other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(A, R, G, B);

    public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";
}

public static class Colors
{
    public static Color Transparent => Color.FromArgb(0, 255, 255, 255);
    public static Color White => Color.FromRgb(255, 255, 255);
    public static Color Black => Color.FromRgb(0, 0, 0);
    public static Color Gray => Color.FromRgb(128, 128, 128);
    public static Color LightGray => Color.FromRgb(211, 211, 211);
    public static Color DarkGray => Color.FromRgb(169, 169, 169);
    public static Color Red => Color.FromRgb(255, 0, 0);
    public static Color Green => Color.FromRgb(0, 128, 0);
    public static Color Blue => Color.FromRgb(0, 0, 255);
    public static Color Yellow => Color.FromRgb(255, 255, 0);
    public static Color Orange => Color.FromRgb(255, 165, 0);
    public static Color Gold => Color.FromRgb(255, 215, 0);
    public static Color Lime => Color.FromRgb(0, 255, 0);
    public static Color Cyan => Color.FromRgb(0, 255, 255);
    public static Color Magenta => Color.FromRgb(255, 0, 255);
    public static Color Purple => Color.FromRgb(128, 0, 128);
}
