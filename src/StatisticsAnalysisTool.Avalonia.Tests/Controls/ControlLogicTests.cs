using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.Controls;

namespace StatisticsAnalysisTool.Avalonia.Tests.Controls;

public class ControlLogicTests
{
    [TestCase("123", true)]
    [TestCase("0", true)]
    [TestCase("12a", false)]
    [TestCase("-1", false)]
    [TestCase("1.5", false)]
    [TestCase(" ", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void DigitsTextBox_AcceptsOnlyNonEmptyDigitText(string text, bool expected)
    {
        DigitsTextBox.IsAllowedText(text).Should().Be(expected);
    }

    [TestCase(5, 100d, 250d, 2)]    // two 100 wide children fit
    [TestCase(5, 100d, 1000d, 5)]   // never more columns than children
    [TestCase(5, 100d, 50d, 1)]     // narrower than a child: one column
    [TestCase(0, 0d, 300d, 1)]      // no children: one column
    [TestCase(4, 0d, 300d, 4)]      // nothing measured yet: one row
    [TestCase(4, 100d, double.PositiveInfinity, 4)]
    public void ResponsiveUniformGrid_CalculatesTheColumnCount(int visibleChildren, double widestChild, double availableWidth, int expected)
    {
        ResponsiveUniformGrid.CalculateColumns(visibleChildren, widestChild, availableWidth).Should().Be(expected);
    }

    [TestCase(double.NaN, 0d, 100d, 0d)]
    [TestCase(double.PositiveInfinity, 0d, 100d, 0d)]
    [TestCase(150d, 0d, 100d, 100d)]
    [TestCase(-5d, 0d, 100d, 0d)]
    [TestCase(40d, 0d, 100d, 40d)]
    public void ProgressBarSmoother_NormalizesTheTargetIntoTheBarRange(double value, double minimum, double maximum, double expected)
    {
        ProgressBarSmoother.NormalizeValue(value, minimum, maximum).Should().Be(expected);
    }

    [TestCase(0, 0d)]
    [TestCase(125, 50d)]
    [TestCase(250, 100d)]
    [TestCase(900, 100d)]
    public void ProgressBarSmoother_InterpolatesLinearlyOverTheDuration(double elapsedMilliseconds, double expected)
    {
        ProgressBarSmoother.ValueAt(0d, 100d, TimeSpan.FromMilliseconds(elapsedMilliseconds)).Should().BeApproximately(expected, 0.001);
    }

    [Test]
    public void ProgressBarSmoother_InterpolatesDownwards()
    {
        ProgressBarSmoother.ValueAt(80d, 20d, TimeSpan.FromMilliseconds(125)).Should().BeApproximately(50d, 0.001);
    }

    [TestCase(80d, 20d, 100d, true)]     // viewport bottom touches the extent
    [TestCase(79.5d, 20d, 100d, true)]   // within a pixel
    [TestCase(50d, 20d, 100d, false)]    // user scrolled up
    [TestCase(0d, 100d, 100d, true)]     // nothing to scroll
    public void ScrollViewerExtensions_DetectsWhenTheViewIsAtTheEnd(double offset, double viewport, double extent, bool expected)
    {
        ScrollViewerExtensions.WasAtEnd(offset, viewport, extent).Should().Be(expected);
    }
}
