using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DSaladin.FontAwesome.WPF;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Avalonia.Tests.Theme;
using StatisticsAnalysisTool.Common.Controls;
using StatisticsAnalysisTool.Linux.Icons;

namespace StatisticsAnalysisTool.Avalonia.Tests.Icons;

public class FontAwesomeImageTests
{
    private const string Namespaces = "xmlns:fa=\"clr-namespace:StatisticsAnalysisTool.Common.Controls\"";

    [TestCase(0, 4, 0d)]
    [TestCase(1000, 4, 90d)]
    [TestCase(2000, 4, 180d)]
    [TestCase(4000, 4, 0d)]      // a full turn wraps
    [TestCase(5000, 4, 90d)]
    [TestCase(1000, 0, 0d)]      // invalid duration: no rotation
    public void SpinAngle_TurnsOnceEveryDurationSeconds(double elapsedMilliseconds, double durationSeconds, double expected)
    {
        FontAwesomeImage.SpinAngle(TimeSpan.FromMilliseconds(elapsedMilliseconds), durationSeconds).Should().BeApproximately(expected, 0.001);
    }

    [AvaloniaTest]
    public void EveryFontStyleHasGlyphsInItsEmbeddedFont()
    {
        // Missing codepoints would render as the font's notdef box, so every mapped codepoint must be in the font's cmap.
        var missing = new List<string>();
        foreach (var name in Enum.GetNames<FontAwesomeIcon>().Where(n => n is not "None" and not "Solid0"))
        {
            var icon = Enum.Parse<FontAwesomeIcon>(name);
            FontAwesomeGlyphs.TryGet(icon, out var glyph);
            if (!FontAwesomeGlyphs.FontHasGlyph(glyph))
            {
                missing.Add(name);
            }
        }

        missing.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void SolidAndRegularVariantsOfTheSameIconHaveDifferentOutlines()
    {
        var solid = FontAwesomeGlyphs.CreateGeometry(FontAwesomeIcon.SolidHeart);
        var regular = FontAwesomeGlyphs.CreateGeometry(FontAwesomeIcon.RegularHeart);

        solid.Should().NotBeNull();
        regular.Should().NotBeNull();
        solid!.Bounds.Width.Should().BeGreaterThan(0);
        solid.Bounds.Should().NotBe(regular!.Bounds, "regular and solid are different font files");
    }

    [AvaloniaTest]
    public void Control_DrawsTheGlyphInItsForeground()
    {
        var window = SharedStylesTests.Load("""<fa:FontAwesomeImage x:Name='icon' Icon="SolidHeart" Width="48" Height="48" Foreground="#FFFF0000" HorizontalAlignment="Left"/>""", Namespaces);

        var red = CountPixels(window, p => p.R > 200 && p.G < 60 && p.B < 60 && p.A > 200);

        red.Should().BeGreaterThan(300, "a 48 px heart fills well over 300 px");
        window.Close();
    }

    [AvaloniaTest]
    public void Control_DrawsNothingForNone_AndFollowsIconChanges()
    {
        var window = SharedStylesTests.Load("""<fa:FontAwesomeImage x:Name='icon' Icon="None" Width="48" Height="48" Foreground="#FFFF0000" HorizontalAlignment="Left"/>""", Namespaces);
        var icon = window.FindControl<FontAwesomeImage>("icon")!;

        CountPixels(window, p => p.R > 200 && p.G < 60 && p.B < 60).Should().Be(0);
        icon.Icon = FontAwesomeIcon.SolidBolt;
        CountPixels(window, p => p.R > 200 && p.G < 60 && p.B < 60).Should().BeGreaterThan(100);
        window.Close();
    }

    [AvaloniaTest]
    public async Task Spin_RotatesWhileEnabledAndResetsWhenDisabled()
    {
        var window = SharedStylesTests.Load("""<fa:FontAwesomeImage x:Name='icon' Icon="SolidRotateRight" Width="32" Height="32" Spin="True" SpinDuration="1"/>""", Namespaces);
        var icon = window.FindControl<FontAwesomeImage>("icon")!;

        await Task.Delay(250);
        icon.CurrentAngle.Should().BeGreaterThan(0).And.BeLessThan(360);
        icon.Spin = false;
        icon.CurrentAngle.Should().Be(0);
        window.Close();
    }

    [AvaloniaTest]
    public void Control_HasADefaultIconSizeAndUsesTheDefaultsOfWpf()
    {
        var window = SharedStylesTests.Load("<fa:FontAwesomeImage x:Name='icon' Icon='SolidBell'/>", Namespaces);
        var icon = window.FindControl<FontAwesomeImage>("icon")!;

        icon.Spin.Should().BeFalse();
        icon.SpinDuration.Should().Be(4);
        icon.Bounds.Width.Should().BeGreaterThan(0);
        window.Close();
    }

    [AvaloniaTest]
    public void IconThemes_ColorTheIconAndFollowTheDisabledState()
    {
        var window = SharedStylesTests.Load("""
            <Button x:Name='on'><fa:FontAwesomeImage x:Name='white' Theme="{StaticResource Style.Button.Icon.White}" Icon="SolidPlus"/></Button>
            <Button x:Name='off' IsEnabled='False'><fa:FontAwesomeImage x:Name='disabled' Theme="{StaticResource Style.Button.Icon.Black}" Icon="SolidPlus"/></Button>
            """, Namespaces);

        ((ISolidColorBrush) window.FindControl<FontAwesomeImage>("white")!.Foreground!).Color.Should().Be(Color("SolidColorBrush.Button.Text.Normal"));
        ((ISolidColorBrush) window.FindControl<FontAwesomeImage>("disabled")!.Foreground!).Color.Should().Be(Color("SolidColorBrush.Button.Text.Disabled"));
        window.Close();
    }

    [AvaloniaTest]
    public void TrackingStateButton_SwapsIconAndTextWhenChecked()
    {
        var window = SharedStylesTests.Load("""
            <ToggleButton x:Name='toggle' Theme="{StaticResource App.TrackingStateButton}"/>
            """, Namespaces);
        var toggle = window.FindControl<ToggleButton>("toggle")!;
        toggle.DataContext = new { Translation = new { TrackingIsNotActive = "Idle", TrackingIsActive = "Tracking" } };
        window.UpdateLayout();

        T Part<T>(string name) where T : global::Avalonia.Visual => toggle.GetVisualDescendants().OfType<T>().First(x => ((Control) (object) x).Name == name);

        Part<FontAwesomeImage>("TrackingStateIconOff").IsVisible.Should().BeTrue();
        Part<FontAwesomeImage>("TrackingStateIconOn").IsVisible.Should().BeFalse();
        Part<TextBlock>("TrackingStateTextOff").Text.Should().Be("Idle");

        toggle.IsChecked = true;
        window.UpdateLayout();

        Part<FontAwesomeImage>("TrackingStateIconOff").IsVisible.Should().BeFalse();
        Part<FontAwesomeImage>("TrackingStateIconOn").IsVisible.Should().BeTrue();
        Part<TextBlock>("TrackingStateTextOn").IsVisible.Should().BeTrue();
        Part<TextBlock>("TrackingStateTextOn").Text.Should().Be("Tracking");
        window.Close();
    }

    private static global::Avalonia.Media.Color Color(string key)
    {
        global::Avalonia.Application.Current!.TryGetResource(key, null, out var value);
        return ((ISolidColorBrush) value!).Color;
    }

    internal static int CountPixelsIn(Window window, Func<global::Avalonia.Media.Color, bool> predicate) => CountPixels(window, predicate);

    private static int CountPixels(Window window, Func<global::Avalonia.Media.Color, bool> predicate)
    {
        window.UpdateLayout();
        using var frame = window.CaptureRenderedFrame()!;
        using var framebuffer = frame.Lock();
        var bgra = framebuffer.Format == PixelFormat.Bgra8888;
        var count = 0;
        var row = new byte[framebuffer.RowBytes];
        for (var y = 0; y < framebuffer.Size.Height; y++)
        {
            System.Runtime.InteropServices.Marshal.Copy(framebuffer.Address + y * framebuffer.RowBytes, row, 0, row.Length);
            for (var x = 0; x < framebuffer.Size.Width; x++)
            {
                var i = x * 4;
                var color = bgra
                    ? global::Avalonia.Media.Color.FromArgb(row[i + 3], row[i + 2], row[i + 1], row[i])
                    : global::Avalonia.Media.Color.FromArgb(row[i + 3], row[i], row[i + 1], row[i + 2]);
                if (predicate(color))
                {
                    count++;
                }
            }
        }

        return count;
    }
}
