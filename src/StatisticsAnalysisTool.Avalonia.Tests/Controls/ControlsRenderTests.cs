using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Avalonia.Tests.Theme;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.Controls;
using StatisticsAnalysisTool.Models.ItemDetailsModel;

namespace StatisticsAnalysisTool.Avalonia.Tests.Controls;

[NonParallelizable]
public class ControlsRenderTests
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

    private const string Namespaces = "xmlns:common=\"clr-namespace:StatisticsAnalysisTool.Common\" xmlns:controls=\"clr-namespace:StatisticsAnalysisTool.Common.Controls\"";

    private static Window Show(string content)
    {
        var window = SharedStylesTests.Load(content, Namespaces);
        return window;
    }

    [AvaloniaTest]
    public void DigitsTextBox_BlocksLettersAndKeepsDigits()
    {
        var window = Show("<controls:DigitsTextBox x:Name='box'/>");
        var box = window.FindControl<DigitsTextBox>("box")!;
        box.Focus();

        window.KeyTextInput("12");
        window.KeyTextInput("ab");
        window.KeyTextInput("-");
        window.KeyTextInput("3");

        box.Text.Should().Be("123");
        box.Height.Should().Be(32, "it reuses the TextBox theme from the shared styles");
        window.Close();
    }

    [AvaloniaTest]
    public void OnlyNumericDouble_FiltersTypedTextLikeTheWpfAttachedProperty()
    {
        var window = Show("<TextBox x:Name='box' common:TextBoxHelper.OnlyNumeric='Double'/>");
        var box = window.FindControl<TextBox>("box")!;
        box.Focus();

        window.KeyTextInput("1");
        window.KeyTextInput("x");
        window.KeyTextInput(".");
        window.KeyTextInput("5");

        TextBoxHelper.GetOnlyNumeric(box).Should().Be(TextBoxHelper.NumericFormat.Double);
        box.Text.Should().Be("1.5");
        window.Close();
    }

    [AvaloniaTest]
    public void OnlyNumericNatural_RejectsAZeroAndDefaultValueCoercesTheText()
    {
        var window = Show("""<TextBox x:Name='box' common:TextBoxHelper.OnlyNumeric='Natural' common:TextBoxHelper.DefaultValue='1'/>""");
        var box = window.FindControl<TextBox>("box")!;
        box.Focus();

        window.KeyTextInput("0");
        box.Text.Should().BeNullOrEmpty();
        window.KeyTextInput("7");
        box.Text.Should().Be("7");
        box.Text = "abc";
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs(); // TextChanged is raised from a posted job
        box.Text.Should().Be("1", "an invalid text is replaced with the default value");
        window.Close();
    }

    [AvaloniaTest]
    public void OnlyNumeric_EvenOddConstraintIsHonoured()
    {
        var window = Show("<TextBox x:Name='box' common:TextBoxHelper.OnlyNumeric='Int' common:TextBoxHelper.EvenOddConstraint='OnlyEven'/>");
        var box = window.FindControl<TextBox>("box")!;
        box.Focus();

        window.KeyTextInput("3");
        window.KeyTextInput("4");

        box.Text.Should().Be("4");
        window.Close();
    }

    [AvaloniaTest]
    public void ResponsiveUniformGrid_UsesAsManyColumnsAsFit()
    {
        var window = Show("""
            <controls:ResponsiveUniformGrid x:Name='grid' Width='250'>
              <Border Width='100' Height='10'/><Border Width='100' Height='10'/><Border Width='100' Height='10'/>
              <Border Width='100' Height='10'/><Border Width='100' Height='10' IsVisible='False'/>
            </controls:ResponsiveUniformGrid>
            """);
        var grid = window.FindControl<ResponsiveUniformGrid>("grid")!;

        grid.Columns.Should().Be(2);
        grid.Width = 1000;
        window.UpdateLayout();
        grid.Columns.Should().Be(4, "only the visible children count");
        window.Close();
    }

    [AvaloniaTest]
    public async Task ProgressBarSmoother_AnimatesTheValueTowardsTheTarget()
    {
        var window = Show("<ProgressBar x:Name='bar' Minimum='0' Maximum='100' Height='10'/>");
        var bar = window.FindControl<ProgressBar>("bar")!;

        ProgressBarSmoother.SetSmoothValue(bar, 80);
        await Task.Delay(120);
        bar.Value.Should().BeGreaterThan(0).And.BeLessThan(80, "the value is interpolated, not set at once");
        await Task.Delay(400);
        bar.Value.Should().Be(80);

        ProgressBarSmoother.SetSmoothValue(bar, 500);
        await Task.Delay(500);
        bar.Value.Should().Be(100, "targets are clamped to the range");
        window.Close();
    }

    [AvaloniaTest]
    public void AlwaysScrollToEnd_ScrollsToTheEndWhenSetAndFollowsNewContent()
    {
        var window = Show("""
            <ScrollViewer x:Name='scroll' Height='100' common:ScrollViewerExtensions.AlwaysScrollToEnd='True'>
              <StackPanel x:Name='items'><Border Height='300'/></StackPanel>
            </ScrollViewer>
            """);
        var scroll = window.FindControl<ScrollViewer>("scroll")!;
        window.UpdateLayout();
        scroll.Offset.Y.Should().BeApproximately(200, 1);

        window.FindControl<StackPanel>("items")!.Children.Add(new Border { Height = 100 });
        window.UpdateLayout();
        scroll.Offset.Y.Should().BeApproximately(300, 1, "the view was at the end, so it follows the new content");

        scroll.Offset = new Vector(0, 50);
        window.UpdateLayout();
        window.FindControl<StackPanel>("items")!.Children.Add(new Border { Height = 100 });
        window.UpdateLayout();
        scroll.Offset.Y.Should().BeApproximately(50, 1, "the user scrolled away from the end");
        window.Close();
    }

    [AvaloniaTest]
    public void SpellDescriptionTextBlock_CreatesRunsWithBoldColorAndTypeBrush()
    {
        var window = Show("<controls:SpellDescriptionTextBlock x:Name='text'/>");
        var text = window.FindControl<SpellDescriptionTextBlock>("text")!;

        text.Segments = new[]
        {
            new ItemSpellDescriptionSegment("plain ", "", false, ""),
            new ItemSpellDescriptionSegment("bold ", "", true, ""),
            new ItemSpellDescriptionSegment("colored ", "", false, "FF8000"),
            new ItemSpellDescriptionSegment("damage", "damage", false, "")
        };

        var runs = text.Inlines!.OfType<Run>().ToList();
        runs.Select(r => r.Text).Should().Equal("plain ", "bold ", "colored ", "damage");
        runs[0].IsSet(TextElement.ForegroundProperty).Should().BeFalse("segments without color or type keep the inherited foreground");
        runs[1].FontWeight.Should().Be(FontWeight.Bold);
        ((ISolidColorBrush) runs[2].Foreground!).Color.Should().Be(Color.FromRgb(0xFF, 0x80, 0x00));
        ((ISolidColorBrush) runs[3].Foreground!).Color.Should().Be(Color.Parse("#FFFF2E2E"));
        text.Segments = null;
        text.Inlines.Should().BeEmpty();
        window.Close();
    }

    [TestCase("heal", "Spell.Type.Heal")]
    [TestCase("movement", "Spell.Type.Mobility")]
    [TestCase("crowdcontrol", "Spell.Type.CrowdControl")]
    [TestCase("nothing", "")]
    public void SpellDescriptionTextBlock_MapsSegmentTypesToBrushKeys(string typeKey, string expected)
    {
        SpellDescriptionTextBlock.GetForegroundResourceKey(typeKey).Should().Be(expected);
    }

    [TestCase("FF8000", true)]
    [TestCase("ff8000", true)]
    [TestCase("FF80", false)]
    [TestCase("GG0000", false)]
    [TestCase(null, false)]
    public void SpellDescriptionTextBlock_ParsesSixDigitHexColors(string hex, bool expected)
    {
        SpellDescriptionTextBlock.TryCreateColorBrush(hex, out var brush).Should().Be(expected);
        (brush != null).Should().Be(expected);
    }

    [AvaloniaTest]
    public void SpellTemplates_RenderTypesStatsAndHideEmptySections()
    {
        var window = SharedStylesTests.Load("""
            <StackPanel>
              <ItemsControl x:Name='types' ItemTemplate="{StaticResource Spell.TypeTemplate}"/>
              <ItemsControl x:Name='stats' ItemTemplate="{StaticResource Spell.StatTemplate}"/>
            </StackPanel>
            """);
        var types = window.FindControl<ItemsControl>("types")!;
        var stats = window.FindControl<ItemsControl>("stats")!;
        types.ItemsSource = new[] { new ItemSpellType("heal", "Heal"), new ItemSpellType("damage", "Damage"), new ItemSpellType("other", "Other") };
        stats.ItemsSource = new[] { new ItemSpellStat("Range", "5 m") };
        window.UpdateLayout();

        var texts = types.GetVisualDescendants().OfType<TextBlock>().ToList();
        texts.Select(t => t.Text).Should().Equal("Heal", "Damage", "Other");
        ((ISolidColorBrush) texts[0].Foreground!).Color.Should().Be(Color.Parse("#FF6ACC04"), "the heal class sets the heal brush");
        ((ISolidColorBrush) texts[1].Foreground!).Color.Should().Be(Color.Parse("#FFFF2E2E"));
        ((ISolidColorBrush) texts[2].Foreground!).Color.Should().NotBe(Color.Parse("#FF6ACC04"));
        stats.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().Equal("Range", "5 m");
        window.Close();
    }
}
