using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common.Converters;
using System.Globalization;

namespace StatisticsAnalysisTool.Avalonia.Tests.Controls;

public class ValueMapConverterTests
{
    private static object Convert(object value, string map, System.Type target) =>
        ValueMapConverter.Instance.Convert(value, target, map, CultureInfo.InvariantCulture);

    [Test]
    public void Convert_PicksTheEntryOfTheValueAndFallsBackToTheDefault()
    {
        Convert("sword", "sword=1|spear=2|*=9", typeof(double)).Should().Be(1d);
        Convert("spear", "sword=1|spear=2|*=9", typeof(double)).Should().Be(2d);
        Convert("other", "sword=1|spear=2|*=9", typeof(double)).Should().Be(9d);
        Convert(null, "sword=1|*=9", typeof(double)).Should().Be(9d);
    }

    [Test]
    public void Convert_WithoutAMatchAndDefaultLeavesThePropertyUnset()
    {
        Convert("x", "a=1", typeof(double)).Should().Be(AvaloniaProperty.UnsetValue);
    }

    [Test]
    public void Convert_ComparesEnumsByNameAndBoolsByInvariantText()
    {
        Convert(System.Windows.Visibility.Visible, "Visible=True|*=False", typeof(bool)).Should().Be(true);
        Convert(false, "True=1|False=2", typeof(int)).Should().Be(2);
    }

    [Test]
    public void Convert_ConvertsTextToStringsBoolsAndFontWeights()
    {
        Convert("a", "a=hello world|*=x", typeof(string)).Should().Be("hello world");
        Convert("a", "a=True|*=False", typeof(bool)).Should().Be(true);
        Convert("a", "a=Bold", typeof(FontWeight)).Should().Be(FontWeight.Bold);
        Convert("a", "a=1,2,3,4", typeof(Thickness)).Should().Be(new Thickness(1, 2, 3, 4));
    }

    [AvaloniaTest]
    public void Convert_ResolvesAtPrefixedEntriesAsApplicationResources()
    {
        var brush = (ISolidColorBrush) Convert("a", "a=@SolidColorBrush.Accent.Red.4", typeof(IBrush));
        brush.Should().NotBeNull();
        var plain = (ISolidColorBrush) Convert("a", "a=#FF0000", typeof(IBrush));
        plain.Color.Should().Be(Colors.Red);
    }
}
