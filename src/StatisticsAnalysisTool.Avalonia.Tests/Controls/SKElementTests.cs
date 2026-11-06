using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using SkiaSharp;
using StatisticsAnalysisTool.Avalonia.Tests.Icons;
using StatisticsAnalysisTool.Common.Controls;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Controls;

public class SKElementTests
{
    [AvaloniaTest]
    public void PaintSurface_DrawsOnTheCanvasAtTheSizeOfTheControl()
    {
        var element = new SKElement { Width = 100, Height = 50 };
        SKImageInfo? seen = null;
        element.PaintSurface += (_, e) =>
        {
            seen = e.Info;
            e.Canvas.Clear(SKColors.Transparent);
            using var paint = new SKPaint { Color = new SKColor(255, 0, 255), Style = SKPaintStyle.Fill };
            e.Canvas.DrawRect(10, 10, 20, 10, paint);
        };
        var window = new Window { Width = 200, Height = 100, Content = element, Background = global::Avalonia.Media.Brushes.Black };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        FontAwesomeImageTests.CountPixelsIn(window, c => c.R > 240 && c.G < 20 && c.B > 240).Should().BeInRange(190, 210, "a 20x10 rectangle");
        seen.Should().NotBeNull();
        seen!.Value.Width.Should().Be(100);
        seen.Value.Height.Should().Be(50);
        window.Close();
    }

    [AvaloniaTest]
    public void InvalidateVisual_PaintsAgainWithTheNewContent()
    {
        var element = new SKElement { Width = 40, Height = 40 };
        var color = SKColors.Red;
        element.PaintSurface += (_, e) =>
        {
            using var paint = new SKPaint { Color = color };
            e.Canvas.DrawRect(0, 0, 40, 40, paint);
        };
        var window = new Window { Width = 100, Height = 100, Content = element, Background = global::Avalonia.Media.Brushes.Black };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        FontAwesomeImageTests.CountPixelsIn(window, c => c.R > 240 && c.G < 20 && c.B < 20).Should().BeGreaterThan(1000);

        color = SKColors.Lime;
        element.InvalidateVisual();
        Dispatcher.UIThread.RunJobs();

        FontAwesomeImageTests.CountPixelsIn(window, c => c.G > 240 && c.R < 20 && c.B < 20).Should().BeGreaterThan(1000);
        window.Close();
    }

    [AvaloniaTest]
    public void PaintSurface_ClearKeepsTheBackdropLikeATransparentWpfBitmap()
    {
        var element = new SKElement { Width = 50, Height = 50 };
        element.PaintSurface += (_, e) => e.Canvas.Clear(SKColors.Transparent);
        var window = new Window { Width = 100, Height = 100, Content = new Panel { Background = global::Avalonia.Media.Brushes.Red, Children = { element } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        FontAwesomeImageTests.CountPixelsIn(window, c => c.R > 240 && c.G < 20 && c.B < 20).Should().Be(100 * 100, "the backdrop shows through the transparent element");
        window.Close();
    }

    [AvaloniaTest]
    public void PaintSurface_ClearOnlyAffectsTheAreaOfTheElement()
    {
        var element = new SKElement { Width = 50, Height = 50, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left };
        element.PaintSurface += (_, e) => e.Canvas.Clear(SKColors.Transparent);
        var backdrop = new Panel { Background = global::Avalonia.Media.Brushes.Red, Children = { element } };
        var window = new Window { Width = 100, Height = 100, Content = backdrop };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var red = FontAwesomeImageTests.CountPixelsIn(window, c => c.R > 240 && c.G < 20 && c.B < 20);
        red.Should().BeGreaterThan(100 * 100 - 50 * 50 - 1, "only the 50x50 element is cleared");
        window.Close();
    }
}
