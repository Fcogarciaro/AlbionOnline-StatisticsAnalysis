using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common.Converters;
using System.Globalization;
using AvaloniaGridLength = Avalonia.Controls.GridLength;
using AvaloniaGridUnitType = Avalonia.Controls.GridUnitType;
using ShimGridLength = System.Windows.GridLength;

namespace StatisticsAnalysisTool.Avalonia.Tests.Controls;

public class GridLengthConverterTests
{
    private static readonly GridLengthConverter Converter = GridLengthConverter.Instance;

    [Test]
    public void Convert_MapsTheShimLengthToTheAvaloniaLength()
    {
        Converter.Convert(new ShimGridLength(2.5, System.Windows.GridUnitType.Star), typeof(AvaloniaGridLength), null, CultureInfo.InvariantCulture)
            .Should().Be(new AvaloniaGridLength(2.5, AvaloniaGridUnitType.Star));
        Converter.Convert(new ShimGridLength(120), typeof(AvaloniaGridLength), null, CultureInfo.InvariantCulture)
            .Should().Be(new AvaloniaGridLength(120, AvaloniaGridUnitType.Pixel));
        Converter.Convert(ShimGridLength.Auto, typeof(AvaloniaGridLength), null, CultureInfo.InvariantCulture)
            .Should().Be(AvaloniaGridLength.Auto);
    }

    [Test]
    public void ConvertBack_MapsTheAvaloniaLengthToTheShimLength()
    {
        Converter.ConvertBack(new AvaloniaGridLength(3, AvaloniaGridUnitType.Star), typeof(ShimGridLength), null, CultureInfo.InvariantCulture)
            .Should().Be(new ShimGridLength(3, System.Windows.GridUnitType.Star));
        Converter.ConvertBack(new AvaloniaGridLength(40), typeof(ShimGridLength), null, CultureInfo.InvariantCulture)
            .Should().Be(new ShimGridLength(40));
    }

    [Test]
    public void Convert_FallsBackToStarForOtherValues()
    {
        Converter.Convert(null, typeof(AvaloniaGridLength), null, CultureInfo.InvariantCulture).Should().Be(new AvaloniaGridLength(1, AvaloniaGridUnitType.Star));
    }
}
