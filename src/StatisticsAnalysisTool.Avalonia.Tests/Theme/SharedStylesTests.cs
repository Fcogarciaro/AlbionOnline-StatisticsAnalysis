using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;

namespace StatisticsAnalysisTool.Avalonia.Tests.Theme;

// Headless render tests of the shared theme (Styles/*.axaml): each XAML snippet is loaded like a ported view would
// (Theme="{StaticResource Key}" / implicit styles) and checked against the palette.
public class SharedStylesTests
{
    internal static Window Load(string content, string windowAttributes = "")
    {
        var window = (Window) AvaloniaRuntimeXamlLoader.Load($"""
            <Window xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Width="400" Height="300" {windowAttributes}>
              <StackPanel>{content}</StackPanel>
            </Window>
            """);
        window.Show();
        return window;
    }

    private static Color Resource(string key)
    {
        global::Avalonia.Application.Current!.TryGetResource(key, null, out var value).Should().BeTrue(key);
        return value is ISolidColorBrush brush ? brush.Color : (Color) value!;
    }

    private static Color ColorOf(IBrush? brush) => ((ISolidColorBrush) brush!).Color;

    [AvaloniaTest]
    public void ImplicitButton_IsThePrimaryButtonAndKeepsItsCustomTemplate()
    {
        var window = Load("<Button x:Name='b' Content='OK'/>");
        var button = window.FindControl<Button>("b")!;

        ColorOf(button.Background).Should().Be(Resource("SolidColorBrush.Button.Primary.Normal"));
        ColorOf(button.BorderBrush).Should().Be(Resource("SolidColorBrush.Button.Primary.Border.Normal"));
        button.Height.Should().Be(32);
        button.MinWidth.Should().Be(40);
        button.GetVisualDescendants().OfType<ContentPresenter>().Should().NotBeEmpty();
        window.Close();
    }

    [AvaloniaTest]
    public void ButtonVariants_UseTheirOwnPalette()
    {
        var window = Load("""
            <Button x:Name='danger' Theme="{StaticResource Style.Button.Danger}" Content='x'/>
            <Button x:Name='attention' Theme="{StaticResource Style.Button.Attention}" Content='x'/>
            <Button x:Name='warning' Theme="{StaticResource Style.Button.Warning}" Content='x'/>
            <Button x:Name='orange' Theme="{StaticResource Style.Button.Orange}" Content='x'/>
            <Button x:Name='neutral' Theme="{StaticResource Style.Button.Neutral}" Content='x'/>
            """);

        ColorOf(window.FindControl<Button>("danger")!.Background).Should().Be(Resource("SolidColorBrush.Button.Danger.Normal"));
        ColorOf(window.FindControl<Button>("attention")!.Background).Should().Be(Resource("SolidColorBrush.Button.Danger.Normal"));
        ColorOf(window.FindControl<Button>("warning")!.Background).Should().Be(Resource("SolidColorBrush.Button.Warning.Normal"));
        ColorOf(window.FindControl<Button>("orange")!.Background).Should().Be(Resource("SolidColorBrush.Button.Orange.Normal"));
        ColorOf(window.FindControl<Button>("neutral")!.Background).Should().Be(Resource("SolidColorBrush.Button.Secondary.Normal"));
        window.Close();
    }

    [AvaloniaTest]
    public void Button_PointerOverPressedAndDisabledSwitchThePalette()
    {
        var window = Load("<Button x:Name='b' Content='OK' Margin='20'/><Button x:Name='d' Content='no' IsEnabled='False'/>");
        var button = window.FindControl<Button>("b")!;
        var disabled = window.FindControl<Button>("d")!;

        ColorOf(disabled.Background).Should().Be(Resource("SolidColorBrush.Button.Primary.Disabled"));
        ColorOf(disabled.Foreground).Should().Be(Resource("SolidColorBrush.Button.Text.Disabled"));

        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center);
        ColorOf(button.Background).Should().Be(Resource("SolidColorBrush.Button.Primary.MouseOver"));
        window.MouseDown(center, global::Avalonia.Input.MouseButton.Left);
        ColorOf(button.Background).Should().Be(Resource("SolidColorBrush.Button.Primary.Pressed"));
        window.MouseUp(center, global::Avalonia.Input.MouseButton.Left);
        window.Close();
    }

    [AvaloniaTest]
    public void KeyedBorderAndTextThemes_ApplyThroughTheThemeProperty()
    {
        var window = Load("""
            <Border x:Name='card' Theme="{StaticResource App.FilterCard}"><TextBlock x:Name='label' Theme="{StaticResource App.FilterLabel}" Text='t'/></Border>
            <Border x:Name='top' Theme="{StaticResource App.TopFilterCard}"/>
            <TextBlock x:Name='title' Theme="{StaticResource Text.Title.1}" Text='t'/>
            <Label x:Name='market' Theme="{StaticResource BlackMarketStyle}" Content='BM'/>
            <TextBlock x:Name='info' Theme="{StaticResource InfoWindow.TextBlock.Title}" Text='t'/>
            """);

        var card = window.FindControl<Border>("card")!;
        ColorOf(card.Background).Should().Be(Resource("SolidColorBrush.Background.T.2"));
        card.CornerRadius.Should().Be(new CornerRadius(5));
        window.FindControl<Border>("top")!.Margin.Should().Be(new Thickness(12, 10, 12, 8), "BasedOn keeps the card values and adds the margin");
        ColorOf(window.FindControl<Border>("top")!.Background).Should().Be(Resource("SolidColorBrush.Background.T.2"));
        window.FindControl<TextBlock>("label")!.FontSize.Should().Be(11);
        window.FindControl<TextBlock>("title")!.FontSize.Should().Be(18);
        var market = window.FindControl<Label>("market")!;
        ColorOf(market.Foreground).Should().Be(Resource("SolidColorBrush.City.Caerleon"));
        market.GetVisualDescendants().OfType<ContentPresenter>().Should().NotBeEmpty("the Fluent Label template is kept through BasedOn");
        window.FindControl<TextBlock>("info")!.FontSize.Should().Be(20);
        window.Close();
    }

    [AvaloniaTest]
    public void ImplicitControls_GetThePaletteAndHeights()
    {
        var window = Load("""
            <TextBox x:Name='text' Text='abc'/>
            <ComboBox x:Name='combo'/>
            <Label x:Name='label' Content='l'/>
            <ProgressBar x:Name='bar' Value='50' Height='40'/>
            <ListBox x:Name='list'/>
            """);

        var text = window.FindControl<TextBox>("text")!;
        text.Height.Should().Be(32);
        ColorOf(text.Background).Should().Be(Resource("SolidColorBrush.Background.7"));
        ColorOf(text.Foreground).Should().Be(Resource("SolidColorBrush.Text.1"));
        window.FindControl<ComboBox>("combo")!.Height.Should().Be(32);
        window.FindControl<ComboBox>("combo")!.MinWidth.Should().Be(150);
        window.FindControl<Label>("label")!.Height.Should().Be(28);
        window.FindControl<ListBox>("list")!.SelectionMode.Should().HaveFlag(SelectionMode.Multiple);
        var bar = window.FindControl<ProgressBar>("bar")!;
        ColorOf(bar.Foreground).Should().Be(Resource("SolidColorBrush.Accent.Blue.3"));
        bar.Bounds.Height.Should().Be(40);
        window.Close();
    }

    [AvaloniaTest]
    public void FluentResourcesBehindTextBoxAndComboBox_AreRecoloredWithThePalette()
    {
        var window = Load("<TextBox x:Name='text'/>");
        var text = window.FindControl<TextBox>("text")!;

        text.TryFindResource("TextControlBackground", text.ActualThemeVariant, out var background).Should().BeTrue();
        ColorOf((IBrush) background!).Should().Be(Resource("SolidColorBrush.Background.7"));
        text.TryFindResource("ComboBoxDropDownBackground", text.ActualThemeVariant, out var dropDown).Should().BeTrue();
        ColorOf((IBrush) dropDown!).Should().Be(Resource("SolidColorBrush.Background.3"));
        window.Close();
    }

    [AvaloniaTest]
    public void CheckBox_ShowsTheMarkOnlyWhenCheckedAndTheDashWhenIndeterminate()
    {
        var window = Load("""
            <CheckBox x:Name='off' Content='a'/>
            <CheckBox x:Name='on' Content='b' IsChecked='True'/>
            <CheckBox x:Name='mixed' Content='c' IsThreeState='True' IsChecked='{x:Null}'/>
            """);

        global::Avalonia.Controls.Shapes.Path Mark(string name) => window.FindControl<CheckBox>(name)!.GetVisualDescendants()
            .OfType<global::Avalonia.Controls.Shapes.Path>().Single();

        Mark("off").IsVisible.Should().BeFalse();
        Mark("on").IsVisible.Should().BeTrue();
        Mark("mixed").IsVisible.Should().BeTrue();
        Mark("mixed").Data.Should().NotBeSameAs(Mark("on").Data, "the indeterminate state draws a dash instead of the check mark");
        window.FindControl<CheckBox>("on")!.Height.Should().Be(21);
        window.Close();
    }

    [AvaloniaTest]
    public void RadioButton_ShowsTheMarkWhenChecked()
    {
        var window = Load("<RadioButton x:Name='a' GroupName='g' Content='a' IsChecked='True'/><RadioButton x:Name='b' GroupName='g' Content='b'/>");

        Border Mark(string name) => window.FindControl<RadioButton>(name)!.GetVisualDescendants().OfType<Border>().Single(x => x.Name == "RadioMark");

        Mark("a").IsVisible.Should().BeTrue();
        Mark("b").IsVisible.Should().BeFalse();
        window.Close();
    }

    [AvaloniaTest]
    public void TabItem_StyledHeaderChangesWithSelection()
    {
        var window = Load("""
            <TabControl x:Name='tabs'>
              <TabItem x:Name='one' Header='One'/>
              <TabItem x:Name='two' Header='Two'/>
            </TabControl>
            """);
        var one = window.FindControl<TabItem>("one")!;
        var two = window.FindControl<TabItem>("two")!;

        one.IsSelected.Should().BeTrue();
        ColorOf(one.Foreground).Should().Be(Resource("SolidColorBrush.Text.1"));
        one.FontWeight.Should().Be(FontWeight.SemiBold);
        ColorOf(two.Foreground).Should().Be(Resource("SolidColorBrush.Text.4"));
        var header = one.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "HeaderBorder");
        ColorOf(header.Background).Should().Be(Resource("SolidColorBrush.Surface.Selected"));
        header.BorderThickness.Should().Be(new Thickness(1, 1, 1, 0));
        window.Close();
    }

    [AvaloniaTest]
    public void HintBars_ProvideTheThreeColorFamilies()
    {
        var window = Load("""
            <Grid x:Name='error' Theme="{StaticResource Grid.Error.Bar}"><Button x:Name='close' Theme="{StaticResource Window.Error.Bar.Button.Style}"/></Grid>
            <Grid x:Name='warn' Theme="{StaticResource Grid.Warning.Bar}"/>
            <Grid x:Name='info' Theme="{StaticResource Grid.Information.Bar}"/>
            <TextBlock Theme="{StaticResource Grid.Warning.Bar.Label}" Text='w'/>
            <TextBlock Theme="{StaticResource Grid.Information.Bar.CopyLink}" Text='c'/>
            <Button Theme="{StaticResource Window.Information.Bar.Button.Style}"/>
            """);

        ColorOf(window.FindControl<Grid>("error")!.Background).Should().Be(Resource("SolidColorBrush.Accent.Red.2"));
        ColorOf(window.FindControl<Grid>("warn")!.Background).Should().Be(Resource("SolidColorBrush.Accent.Yellow.3"));
        ColorOf(window.FindControl<Grid>("info")!.Background).Should().Be(Resource("SolidColorBrush.Accent.Blue.3"));
        window.FindControl<Grid>("error")!.Height.Should().Be(70);
        var close = window.FindControl<Button>("close")!;
        ColorOf(close.Foreground).Should().Be(Resource("SolidColorBrush.Accent.Red.5"));
        close.GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Path>().Should().ContainSingle("the Webdings close glyph is drawn as a path");
        window.Close();
    }

    [AvaloniaTest]
    public void FilterToggleButton_TemplateReactsToTheCheckedState()
    {
        var window = Load("""
            <ToggleButton x:Name='off' Theme="{StaticResource App.FilterToggleButton}" Content='a'/>
            <ToggleButton x:Name='on' Theme="{StaticResource App.FilterToggleButton}" Content='b' IsChecked='True'/>
            <ToggleButton x:Name='drop' Theme="{StaticResource App.FilterDropdownButton}" Content='c'/>
            """);

        Border Surface(string name, string part) => window.FindControl<ToggleButton>(name)!.GetVisualDescendants().OfType<Border>().First(b => b.Name == part);

        ColorOf(Surface("off", "FilterSurface").Background).Should().Be(Resource("SolidColorBrush.Button.Secondary.Normal"));
        ColorOf(Surface("on", "FilterSurface").Background).Should().Be(Resource("SolidColorBrush.Accent.Blue.5"));
        ColorOf(window.FindControl<ToggleButton>("on")!.Foreground).Should().Be(Resource("SolidColorBrush.Text.1"));
        ColorOf(Surface("drop", "Border").Background).Should().Be(Resource("SolidColorBrush.Background.2"));
        window.Close();
    }

    [AvaloniaTest]
    public void WindowChromeTheme_KeepsTheWindowTemplateAndSetsThePalette()
    {
        var window = Load("<TextBlock Text='x'/>", "Theme=\"{StaticResource WindowChrome}\"");

        ColorOf(window.Background).Should().Be(Resource("SolidColorBrush.Background.2"));
        window.FontSize.Should().Be(12);
        window.ExtendClientAreaChromeHints.Should().Be(global::Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome);
        window.Close();
    }

    [AvaloniaTest]
    public void SharedControls_RenderToAFrame()
    {
        var window = Load("""
            <Button Content='Primary'/>
            <ToggleButton Theme="{StaticResource App.FilterToggleButton}" Content='Filter' IsChecked='True'/>
            <CheckBox Content='check' IsChecked='True'/>
            <RadioButton Content='radio' IsChecked='True'/>
            <ComboBox><ComboBoxItem Content='one'/></ComboBox>
            <TextBox Text='text'/>
            <ProgressBar Value='30'/>
            <TabControl><TabItem Header='tab'/></TabControl>
            """);

        var frame = window.CaptureRenderedFrame();

        frame.Should().NotBeNull();
        frame!.PixelSize.Width.Should().BeGreaterThan(100);
        window.Close();
    }
}
