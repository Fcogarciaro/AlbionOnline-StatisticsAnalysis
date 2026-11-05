using System.Windows;
using FluentAssertions;
using NUnit.Framework;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class MessageBoxLayoutTests
{
    [TestCase(MessageBoxButton.OK, new[] { MessageBoxResult.OK })]
    [TestCase(MessageBoxButton.OKCancel, new[] { MessageBoxResult.OK, MessageBoxResult.Cancel })]
    [TestCase(MessageBoxButton.YesNo, new[] { MessageBoxResult.Yes, MessageBoxResult.No })]
    [TestCase(MessageBoxButton.YesNoCancel, new[] { MessageBoxResult.Yes, MessageBoxResult.No, MessageBoxResult.Cancel })]
    public void Choices_MapEachButtonSetToItsResultsInOrder(MessageBoxButton button, MessageBoxResult[] expected)
    {
        MessageBoxLayout.Choices(button).Select(c => c.Result).Should().Equal(expected);
    }

    [TestCase(MessageBoxButton.OK, MessageBoxResult.OK)]
    [TestCase(MessageBoxButton.OKCancel, MessageBoxResult.Cancel)]
    [TestCase(MessageBoxButton.YesNo, MessageBoxResult.No)]
    [TestCase(MessageBoxButton.YesNoCancel, MessageBoxResult.Cancel)]
    public void CloseResult_IsTheCancelLikeButton(MessageBoxButton button, MessageBoxResult expected)
    {
        MessageBoxLayout.CloseResult(button).Should().Be(expected);
    }

    [Test]
    public void IconGlyph_NoneHasNoBadge_OthersDo()
    {
        MessageBoxLayout.IconGlyph(MessageBoxImage.None).Should().BeNull();
        new[] { MessageBoxImage.Error, MessageBoxImage.Warning, MessageBoxImage.Information, MessageBoxImage.Question }
            .Select(MessageBoxLayout.IconGlyph).Should().OnlyContain(g => !string.IsNullOrEmpty(g)).And.OnlyHaveUniqueItems();
    }
}
