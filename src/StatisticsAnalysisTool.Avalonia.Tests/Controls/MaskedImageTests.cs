using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common.Controls;
using StatisticsAnalysisTool.Common.Converters;
using System.Globalization;

namespace StatisticsAnalysisTool.Avalonia.Tests.Controls;

public class MaskedImageTests
{
    [AvaloniaTest]
    public void Render_PaintsTheFillThroughTheIconSilhouette()
    {
        var source = ImagePathToBitmapConverter.Instance.Convert(
            "pack://application:,,,/Assets/Navigation/dashboard.png", typeof(object), null, CultureInfo.InvariantCulture);
        var image = new MaskedImage { Source = (global::Avalonia.Media.IImageBrushSource) source, Fill = Brushes.Red, Width = 32, Height = 32 };
        var window = new Window { Width = 64, Height = 64, Background = Brushes.Black, Content = new Grid { Children = { image } } };
        window.Show();

        var frame = window.CaptureRenderedFrame();

        frame.Should().NotBeNull();
        var painted = 0;
        using var buffer = frame!.Lock();
        unsafe
        {
            var pixels = (byte*) buffer.Address;
            for (var i = 0; i < buffer.Size.Width * buffer.Size.Height; i++)
            {
                // RGBA: red above blue means the Red fill reached the pixel.
                if (pixels[i * 4] > 100 && pixels[i * 4 + 2] < 50)
                {
                    painted++;
                }
            }
        }

        painted.Should().BeGreaterThan(20, "the silhouette of the icon is filled with the brush");
        window.Close();
    }

    [AvaloniaTest]
    public void Render_WithoutSourceDrawsNothingAndDoesNotThrow()
    {
        var window = new Window { Width = 40, Height = 40, Content = new MaskedImage { Fill = Brushes.Red } };
        window.Show();

        window.CaptureRenderedFrame().Should().NotBeNull();
        window.Close();
    }
}
