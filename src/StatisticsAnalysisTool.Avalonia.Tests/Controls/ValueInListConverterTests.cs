using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common.Converters;
using System.Globalization;

namespace StatisticsAnalysisTool.Avalonia.Tests.Controls;

public class ValueInListConverterTests
{
    private static bool Convert(object value, string list) => (bool) ValueInListConverter.Instance.Convert(value, typeof(bool), list, CultureInfo.InvariantCulture);

    [TestCase("Thetford", true)]
    [TestCase("Bank of Thetford", true)]
    [TestCase("Thetford Portal", true)]
    [TestCase("thetford", false)]
    [TestCase("Thetford ", false)]
    [TestCase("Martlock", false)]
    [TestCase(null, false)]
    public void Convert_MatchesTheValueExactlyAgainstThePipeSeparatedList(string value, bool expected)
    {
        Convert(value, "Thetford|Thetford Portal|Bank of Thetford").Should().Be(expected);
    }

    [Test]
    public void Convert_KeepsApostrophesAndComparesNonStringsByInvariantText()
    {
        Convert("Arthur's Rest", "Arthur's Rest").Should().BeTrue();
        Convert(12, "11|12").Should().BeTrue();
        Convert(1.5, "1.5").Should().BeTrue();
    }

    [Test]
    public void Convert_WithoutAListIsFalse()
    {
        Convert("x", null).Should().BeFalse();
    }
}
