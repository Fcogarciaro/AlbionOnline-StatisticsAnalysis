using System.Windows.Media.Imaging;
using System.Net.Http;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat.Media;

public class BitmapImageTests
{
    // 1x1 transparent PNG
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    [Test]
    [TestCase("pack://application:,,,/Assets/foo.png", "avares://StatisticsAnalysisTool/Assets/foo.png")]
    [TestCase("pack://application:,,,/StatisticsAnalysisTool;component/Resources/Trash.png", "avares://StatisticsAnalysisTool/Resources/Trash.png")]
    [TestCase("pack://application:,,,/Other.Assembly;component/a/b.png", "avares://Other.Assembly/a/b.png")]
    public void MapPackUri_ConvertsToAvares(string pack, string expected)
    {
        BitmapImage.MapPackUri(new Uri(pack, UriKind.Absolute)).ToString().Should().Be(expected);
    }

    [Test]
    public void MapPackUri_LeavesOtherSchemesUntouched()
    {
        var file = new Uri("file:///tmp/x.png");

        BitmapImage.MapPackUri(file).Should().Be(file);
        BitmapImage.MapPackUri(new Uri("https://render.albiononline.com/v1/item/T4_BAG")).Scheme.Should().Be("https");
    }

    [AvaloniaTest]
    public void Init_RequiresBeginInitBeforeSettingProperties()
    {
        var image = new BitmapImage();
        using var stream = new MemoryStream(Png);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();

        var mutate = () => image.CacheOption = BitmapCacheOption.None;
        mutate.Should().Throw<InvalidOperationException>("initialization is complete");
    }

    [Test]
    public void EndInit_WithoutSource_Throws()
    {
        var image = new BitmapImage();
        image.BeginInit();

        var end = () => image.EndInit();

        end.Should().Throw<InvalidOperationException>();
    }

    [AvaloniaTest]
    public void EndInit_StreamSource_DecodesAndCompletesSynchronously()
    {
        using var stream = new MemoryStream(Png);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();

        image.IsDownloading.Should().BeFalse();
        image.PixelWidth.Should().Be(1);
        image.PixelHeight.Should().Be(1);
        image.AvaloniaBitmap.Should().NotBeNull();
    }

    [AvaloniaTest]
    public void EndInit_FileUri_LoadsBitmap()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sat-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, Png);
        try
        {
            var image = new BitmapImage(new Uri(path));

            image.PixelWidth.Should().Be(1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [AvaloniaTest]
    public void DecodePixelWidth_ScalesBitmap()
    {
        using var stream = new MemoryStream(Png);
        var image = new BitmapImage();
        image.BeginInit();
        image.DecodePixelWidth = 4;
        image.DecodePixelHeight = 6;
        image.StreamSource = stream;
        image.EndInit();

        image.PixelWidth.Should().Be(4);
        image.PixelHeight.Should().Be(6);
    }

    [AvaloniaTest]
    public void Freeze_MakesFrozen_AndFrozenImageRejectsBeginInit()
    {
        using var stream = new MemoryStream(Png);
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = stream;
        image.EndInit();
        image.CanFreeze.Should().BeTrue();

        image.Freeze();

        image.IsFrozen.Should().BeTrue();
        var begin = () => image.BeginInit();
        begin.Should().Throw<InvalidOperationException>();
    }

    [AvaloniaTest]
    public void PngEncoder_SavesFrameThatDecodesAgain()
    {
        using var stream = new MemoryStream(Png);
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = stream;
        image.EndInit();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var output = new MemoryStream();
        encoder.Save(output);

        output.Length.Should().BeGreaterThan(8);
        output.ToArray().Take(4).Should().Equal(0x89, 0x50, 0x4E, 0x47);
    }

    [AvaloniaTest]
    public async Task HttpSource_DownloadsAsynchronously_AndRaisesDownloadCompleted()
    {
        var previous = BitmapImage.Downloader;
        BitmapImage.Downloader = _ => Task.FromResult(Png);
        try
        {
            var completed = 0;
            var image = new BitmapImage();
            image.DownloadCompleted += (_, _) => completed++;
            image.BeginInit();
            image.UriSource = new Uri("https://render.albiononline.com/v1/item/T4_BAG");
            image.EndInit();

            image.IsDownloading.Should().BeTrue();
            image.CanFreeze.Should().BeFalse("WPF cannot freeze while downloading");
            await image.LoadTask;
            AvaloniaDispatcher.UIThread.RunJobs();

            image.IsDownloading.Should().BeFalse();
            completed.Should().Be(1);
            image.PixelWidth.Should().Be(1);
        }
        finally
        {
            BitmapImage.Downloader = previous;
        }
    }

    [AvaloniaTest]
    public async Task HttpSource_Failure_RaisesDownloadFailed()
    {
        var previous = BitmapImage.Downloader;
        BitmapImage.Downloader = _ => throw new HttpRequestException("offline");
        try
        {
            Exception captured = null;
            var image = new BitmapImage();
            image.DownloadFailed += (_, e) => captured = e.ErrorException;
            image.BeginInit();
            image.UriSource = new Uri("https://example.invalid/x.png");
            image.EndInit();

            await image.LoadTask;
            AvaloniaDispatcher.UIThread.RunJobs();

            image.IsDownloading.Should().BeFalse();
            captured.Should().BeOfType<HttpRequestException>();
        }
        finally
        {
            BitmapImage.Downloader = previous;
        }
    }

    [AvaloniaTest]
    public async Task HttpSource_LoadTask_CompletesOnlyAfterTheBitmapIsPublished()
    {
        var previous = BitmapImage.Downloader;
        BitmapImage.Downloader = _ => Task.FromResult(Png);
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri("https://render.albiononline.com/v1/item/T4_BAG");
            image.EndInit();

            await image.LoadTask;

            image.AvaloniaBitmap.Should().NotBeNull();
            image.IsDownloading.Should().BeFalse();
        }
        finally
        {
            BitmapImage.Downloader = previous;
        }
    }
}
