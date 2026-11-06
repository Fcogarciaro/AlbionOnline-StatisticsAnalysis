using System.Windows.Media;
using FluentAssertions;
using NUnit.Framework;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat.Media;

public class ColorAndBrushTests
{
    [Test]
    public void FromRgb_IsOpaque_FromArgb_KeepsAlpha()
    {
        var rgb = Color.FromRgb(1, 2, 3);
        var argb = Color.FromArgb(4, 1, 2, 3);

        (rgb.A, rgb.R, rgb.G, rgb.B).Should().Be(((byte) 255, (byte) 1, (byte) 2, (byte) 3));
        argb.A.Should().Be(4);
        rgb.Should().NotBe(argb);
        (Color.FromRgb(1, 2, 3) == rgb).Should().BeTrue();
    }

    [Test]
    public void ToString_UsesWpfHexFormat()
    {
        Color.FromArgb(0x80, 0x12, 0xAB, 0xFF).ToString().Should().Be("#8012ABFF");
    }

    [Test]
    public void AvaloniaConversion_RoundTrips()
    {
        var color = Color.FromArgb(10, 20, 30, 40);

        var avalonia = color.ToAvalonia();

        (avalonia.A, avalonia.R, avalonia.G, avalonia.B).Should().Be(((byte) 10, (byte) 20, (byte) 30, (byte) 40));
        Color.FromAvalonia(avalonia).Should().Be(color);
    }

    [Test]
    public void NamedColorsAndBrushes_MatchWpf()
    {
        Colors.Transparent.A.Should().Be(0);
        Colors.White.Should().Be(Color.FromRgb(255, 255, 255));
        Colors.Gray.Should().Be(Color.FromRgb(128, 128, 128));
        Brushes.Transparent.Color.A.Should().Be(0);
        Brushes.White.IsFrozen.Should().BeTrue();
    }

    [Test]
    public void SolidColorBrush_ToAvalonia_CarriesColorAndOpacity()
    {
        var brush = new SolidColorBrush(Color.FromRgb(9, 8, 7)) { Opacity = 0.5 };

        var avalonia = (global::Avalonia.Media.ISolidColorBrush) brush.ToAvalonia();

        avalonia.Color.R.Should().Be(9);
        avalonia.Opacity.Should().Be(0.5);
    }

    [Test]
    public void FrozenBrush_RejectsMutation()
    {
        var brush = new SolidColorBrush(Colors.Red);
        brush.CanFreeze.Should().BeTrue();

        brush.Freeze();

        brush.IsFrozen.Should().BeTrue();
        var mutate = () => brush.Color = Colors.Blue;
        mutate.Should().Throw<InvalidOperationException>();
    }
}
