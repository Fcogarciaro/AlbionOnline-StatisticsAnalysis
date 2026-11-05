using Avalonia;
using NUnit.Framework;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.Converters;
using System.Globalization;
using System.Windows;
using AvaloniaWindowState = global::Avalonia.Controls.WindowState;

namespace StatisticsAnalysisTool.Avalonia.Tests.Chrome;

public class ChromeLogicTests
{
    [AvaloniaTest]
    [TestCase("0")]
    [TestCase("1")]
    [TestCase("2")]
    [TestCase("r")]
    public void ChromeGlyph_DrawsTheWebdingsContentOfTheWindowButtons(string content)
    {
        var geometry = ChromeGlyphConverter.Instance.Convert(content, typeof(object), null, CultureInfo.InvariantCulture);

        geometry.Should().BeAssignableTo<global::Avalonia.Media.Geometry>();
    }

    [AvaloniaTest]
    public void ChromeGlyph_ReturnsNothingForUnknownContent()
    {
        ChromeGlyphConverter.Instance.Convert("x", typeof(object), null, CultureInfo.InvariantCulture).Should().BeNull();
        ChromeGlyphConverter.Instance.Convert(null, typeof(object), null, CultureInfo.InvariantCulture).Should().BeNull();
        ChromeGlyphConverter.Instance.Convert(2, typeof(object), null, CultureInfo.InvariantCulture)
            .Should().BeAssignableTo<global::Avalonia.Media.Geometry>("the controller sets an int, as upstream");
    }

    [AvaloniaTest]
    public void ImagePath_LoadsPackAndAvaresUris()
    {
        var converter = ImagePathToBitmapConverter.Instance;

        converter.Convert("pack://application:,,,/Assets/Navigation/dashboard.png", typeof(object), null, CultureInfo.InvariantCulture)
            .Should().BeAssignableTo<global::Avalonia.Media.Imaging.Bitmap>();
        converter.Convert("avares://StatisticsAnalysisTool/Assets/heart.png", typeof(object), null, CultureInfo.InvariantCulture)
            .Should().BeAssignableTo<global::Avalonia.Media.Imaging.Bitmap>();
        converter.Convert("pack://application:,,,/Assets/missing.png", typeof(object), null, CultureInfo.InvariantCulture).Should().BeNull();
        converter.Convert(null, typeof(object), null, CultureInfo.InvariantCulture).Should().BeNull();
    }

    [Test]
    public void ManualDrag_MovesTheWindowByThePointerDelta()
    {
        var drag = new ManualWindowDrag();
        drag.Begin(new PixelPoint(100, 100), new PixelPoint(500, 300));

        drag.Move(new PixelPoint(130, 90)).Should().Be(new PixelPoint(530, 290));
        drag.Move(new PixelPoint(90, 100)).Should().Be(new PixelPoint(490, 300));
        drag.End();
        drag.IsActive.Should().BeFalse();
        drag.Move(new PixelPoint(0, 0)).Should().BeNull("an ended drag ignores the pointer");
    }

    [AvaloniaTest]
    public void Controller_TogglesMaximizeAndTheButtonGlyph()
    {
        var window = new System.Windows.Window { Width = 300, Height = 200 };
        var button = new Button();
        window.Content = button;
        window.Show();
        var controller = new WindowChromeController(window, button, ResizeMode.CanResizeWithGrip, ResizeMode.NoResize);

        button.Content.Should().Be(1);
        controller.ToggleMaximize();
        window.WindowState.Should().Be(AvaloniaWindowState.Maximized);
        window.CanResize.Should().BeFalse("maximized uses ResizeMode.NoResize");
        button.Content.Should().Be(2);

        controller.ToggleMaximize();
        window.WindowState.Should().Be(AvaloniaWindowState.Normal);
        window.CanResize.Should().BeTrue();
        button.Content.Should().Be(1);
        window.Close();
    }
}

public class ToStringEqualsConverterTests
{
    [TestCase(1, "1", true)]
    [TestCase(2, "1", false)]
    [TestCase(null, "1", false)]
    [TestCase(3, null, false)]
    public void Compares_ValueAndParameterAsInvariantText(object value, string parameter, bool expected)
    {
        ToStringEqualsConverter.Instance.Convert(value, typeof(bool), parameter, CultureInfo.InvariantCulture).Should().Be(expected);
    }
}

public class CompatBrushConverterTests
{
    [Test]
    public void Converts_TheShimBrushToAnAvaloniaBrushAndPassesOthersThrough()
    {
        var shim = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);

        var converted = CompatBrushConverter.Instance.Convert(shim, typeof(global::Avalonia.Media.IBrush), null, CultureInfo.InvariantCulture);

        converted.Should().BeAssignableTo<global::Avalonia.Media.ISolidColorBrush>()
            .Which.Color.Should().Be(global::Avalonia.Media.Colors.Red);
        var avalonia = global::Avalonia.Media.Brushes.Blue;
        CompatBrushConverter.Instance.Convert(avalonia, typeof(object), null, CultureInfo.InvariantCulture).Should().BeSameAs(avalonia);
        CompatBrushConverter.Instance.Convert(null, typeof(object), null, CultureInfo.InvariantCulture).Should().BeNull();
    }
}
