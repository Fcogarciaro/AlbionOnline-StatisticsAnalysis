using System.Globalization;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common;
using static StatisticsAnalysisTool.Common.TextBoxHelper;

namespace StatisticsAnalysisTool.Avalonia.Tests.Controls;

[NonParallelizable]
public class NumericInputRulesTests
{
    private CultureInfo _previous;

    [SetUp]
    public void SetUp()
    {
        _previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
    }

    [TearDown]
    public void TearDown() => CultureInfo.CurrentCulture = _previous;

    [TestCase("12", true)]
    [TestCase("12.5", true)]
    [TestCase("12,5", true)]    // alternate separator is normalized
    [TestCase("-3", true)]
    [TestCase("1.", true)]      // a separator at the end is a number being typed
    [TestCase("1,", true)]
    [TestCase(".", true)]
    [TestCase("", false)]
    [TestCase("abc", false)]
    [TestCase("1.2.3", false)]
    public void Double_AcceptsNumbersAndPendingDecimalInput(string text, bool expected)
    {
        NumericInputRules.IsAllowed(text, NumericFormat.Double, EvenOddConstraint.All).Should().Be(expected);
    }

    [TestCase("12", true)]
    [TestCase("-12", true)]
    [TestCase("1.5", false)]
    [TestCase("", false)]
    public void Int_AcceptsOnlyIntegers(string text, bool expected)
    {
        NumericInputRules.IsAllowed(text, NumericFormat.Int, EvenOddConstraint.All).Should().Be(expected);
    }

    [TestCase("0", true)]
    [TestCase("7", true)]
    [TestCase("-1", false)]
    public void Uint_RejectsNegativeNumbers(string text, bool expected)
    {
        NumericInputRules.IsAllowed(text, NumericFormat.Uint, EvenOddConstraint.All).Should().Be(expected);
    }

    [TestCase("0", false)]
    [TestCase("1", true)]
    [TestCase("-5", false)]
    public void Natural_RejectsZero(string text, bool expected)
    {
        NumericInputRules.IsAllowed(text, NumericFormat.Natural, EvenOddConstraint.All).Should().Be(expected);
    }

    [TestCase("4", EvenOddConstraint.OnlyEven, true)]
    [TestCase("3", EvenOddConstraint.OnlyEven, false)]
    [TestCase("3", EvenOddConstraint.OnlyOdd, true)]
    [TestCase("4", EvenOddConstraint.OnlyOdd, false)]
    public void EvenOddConstraintIsApplied(string text, EvenOddConstraint constraint, bool expected)
    {
        NumericInputRules.IsAllowed(text, NumericFormat.Int, constraint).Should().Be(expected);
        NumericInputRules.IsAllowed(text, NumericFormat.Double, constraint).Should().Be(expected);
    }

    [Test]
    public void GermanCulture_UsesTheCommaAsDecimalSeparator()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        NumericInputRules.IsAllowed("1,5", NumericFormat.Double, EvenOddConstraint.All).Should().BeTrue();
        NumericInputRules.IsAllowed("1.5", NumericFormat.Double, EvenOddConstraint.All).Should().BeTrue("the alternate separator is accepted too");
    }

    [Test]
    public void Insert_ReplacesTheSelectionThenInsertsTheTypedText()
    {
        NumericInputRules.Insert("12345", 1, 2, "9").Should().Be("1945");
        NumericInputRules.Insert("12", 2, 0, "3").Should().Be("123");
        NumericInputRules.Insert(null, 0, 0, "3").Should().Be("3");
    }

    [TestCase("5", NumericFormat.Natural, "1", "5")]
    [TestCase("0", NumericFormat.Natural, "1", "1")]
    [TestCase("abc", NumericFormat.Int, "0", "0")]
    [TestCase("", NumericFormat.Double, "0", "0")]
    [TestCase("1.", NumericFormat.Double, "0", "1.")]
    public void Coerce_ReplacesInvalidTextWithTheDefaultValue(string text, NumericFormat format, string defaultValue, string expected)
    {
        NumericInputRules.Coerce(text, format, EvenOddConstraint.All, defaultValue).Should().Be(expected);
    }

    [Test]
    public void Coerce_AppliesTheEvenOddConstraint()
    {
        NumericInputRules.Coerce("3", NumericFormat.Uint, EvenOddConstraint.OnlyEven, "2").Should().Be("2");
        NumericInputRules.Coerce("4", NumericFormat.Uint, EvenOddConstraint.OnlyEven, "2").Should().Be("4");
    }
}
