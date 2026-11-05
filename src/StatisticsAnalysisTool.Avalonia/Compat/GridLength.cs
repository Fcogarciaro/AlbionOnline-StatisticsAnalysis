// Compatibility shim: System.Windows.GridLength / GridUnitType mapped to Avalonia.Controls.GridLength.
namespace System.Windows;

public enum GridUnitType
{
    Auto = 0,
    Pixel = 1,
    Star = 2
}

public readonly struct GridLength : IEquatable<GridLength>
{
    public GridLength(double pixels) : this(pixels, GridUnitType.Pixel)
    {
    }

    public GridLength(double value, GridUnitType type)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentException("Value must be a finite, non-negative number.", nameof(value));
        }

        Value = type == GridUnitType.Auto ? 1.0 : value;
        GridUnitType = type;
    }

    public static GridLength Auto { get; } = new(1.0, GridUnitType.Auto);

    public double Value { get; }

    public GridUnitType GridUnitType { get; }

    public bool IsAuto => GridUnitType == GridUnitType.Auto;

    public bool IsAbsolute => GridUnitType == GridUnitType.Pixel;

    public bool IsStar => GridUnitType == GridUnitType.Star;

    public Avalonia.Controls.GridLength ToAvalonia() => GridUnitType switch
    {
        GridUnitType.Auto => Avalonia.Controls.GridLength.Auto,
        GridUnitType.Star => new Avalonia.Controls.GridLength(Value, Avalonia.Controls.GridUnitType.Star),
        _ => new Avalonia.Controls.GridLength(Value, Avalonia.Controls.GridUnitType.Pixel)
    };

    public static GridLength FromAvalonia(Avalonia.Controls.GridLength length) => length.GridUnitType switch
    {
        Avalonia.Controls.GridUnitType.Auto => Auto,
        Avalonia.Controls.GridUnitType.Star => new GridLength(length.Value, GridUnitType.Star),
        _ => new GridLength(length.Value)
    };

    public static implicit operator Avalonia.Controls.GridLength(GridLength length) => length.ToAvalonia();

    public static implicit operator GridLength(Avalonia.Controls.GridLength length) => FromAvalonia(length);

    public static bool operator ==(GridLength left, GridLength right) => left.Equals(right);

    public static bool operator !=(GridLength left, GridLength right) => !left.Equals(right);

    public bool Equals(GridLength other) => GridUnitType == other.GridUnitType && (IsAuto || Value.Equals(other.Value));

    public override bool Equals(object obj) => obj is GridLength other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Value, GridUnitType);

    public override string ToString() => GridUnitType switch
    {
        GridUnitType.Auto => "Auto",
        GridUnitType.Star => Value == 1.0 ? "*" : FormattableString.Invariant($"{Value}*"),
        _ => FormattableString.Invariant($"{Value}")
    };
}
