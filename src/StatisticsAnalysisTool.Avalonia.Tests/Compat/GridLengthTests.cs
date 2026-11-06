using System.Windows;
using FluentAssertions;
using NUnit.Framework;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat;

public class GridLengthTests
{
    [Test]
    public void PixelConstructor_IsAbsolute()
    {
        var length = new GridLength(120);

        (length.IsAbsolute, length.IsAuto, length.IsStar, length.Value).Should().Be((true, false, false, 120d));
    }

    [Test]
    public void Auto_HasWpfShape_AndEqualityIgnoresValue()
    {
        GridLength.Auto.IsAuto.Should().BeTrue();
        GridLength.Auto.Value.Should().Be(1);
        (new GridLength(5, GridUnitType.Auto) == GridLength.Auto).Should().BeTrue();
        new GridLength(5).Should().NotBe(new GridLength(5, GridUnitType.Star));
    }

    [Test]
    public void NegativeOrNonFinite_Throws()
    {
        var negative = () => new GridLength(-1);
        var nan = () => new GridLength(double.NaN);

        negative.Should().Throw<ArgumentException>();
        nan.Should().Throw<ArgumentException>();
    }

    [Test]
    public void AvaloniaRoundTrip_PreservesUnitAndValue()
    {
        foreach (var length in new[] { GridLength.Auto, new GridLength(42), new GridLength(2.5, GridUnitType.Star) })
        {
            GridLength.FromAvalonia(length.ToAvalonia()).Should().Be(length);
        }

        new GridLength(3, GridUnitType.Star).ToAvalonia().IsStar.Should().BeTrue();
    }

    [Test]
    public void ToString_MatchesWpfFormat()
    {
        GridLength.Auto.ToString().Should().Be("Auto");
        new GridLength(10).ToString().Should().Be("10");
        new GridLength(1, GridUnitType.Star).ToString().Should().Be("*");
        new GridLength(2, GridUnitType.Star).ToString().Should().Be("2*");
    }
}
