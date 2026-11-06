using Avalonia;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common.Controls;
using System.Net.Http;
using System.Windows.Media.Imaging;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Controls;

[NonParallelizable]
public class UrlImageTests
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("file:///etc/passwd")]
    [TestCase("javascript:alert(1)")]
    [TestCase("not a url")]
    public void IsLoadableUrl_AcceptsOnlyAbsoluteHttpAndHttps(string url)
    {
        UrlImage.TryGetWebUri(url, out _).Should().BeFalse();
    }

    [TestCase("https://render.albiononline.com/v1/avatar/A")]
    [TestCase("http://example.com/a.png")]
    public void IsLoadableUrl_AcceptsWebUrls(string url)
    {
        UrlImage.TryGetWebUri(url, out var uri).Should().BeTrue();
        uri.AbsoluteUri.Should().Be(new Uri(url).AbsoluteUri);
    }

    [AvaloniaTest]
    public async Task Url_LoadsTheImageAsynchronouslyIntoSource()
    {
        var previous = BitmapImage.Downloader;
        var gate = new TaskCompletionSource<byte[]>();
        BitmapImage.Downloader = _ => gate.Task;
        try
        {
            var image = new UrlImage { Url = "https://render.albiononline.com/v1/avatar/OK" };
            image.Source.Should().BeNull("the download has not finished");

            gate.SetResult(Png);
            await image.LoadTask;
            AvaloniaDispatcher.UIThread.RunJobs();

            image.Source.Should().NotBeNull();
            image.Source!.Size.Should().Be(new Size(1, 1));
        }
        finally
        {
            BitmapImage.Downloader = previous;
        }
    }

    [AvaloniaTest]
    public async Task ChangingTheUrlToSomethingInvalidClearsTheSource()
    {
        var previous = BitmapImage.Downloader;
        BitmapImage.Downloader = _ => Task.FromResult(Png);
        try
        {
            var image = new UrlImage { Url = "https://render.albiononline.com/v1/avatar/CLEAR" };
            await image.LoadTask;
            AvaloniaDispatcher.UIThread.RunJobs();
            image.Source.Should().NotBeNull();

            image.Url = null;

            image.Source.Should().BeNull();
        }
        finally
        {
            BitmapImage.Downloader = previous;
        }
    }

    [AvaloniaTest]
    public async Task AFailedDownloadLeavesTheSourceEmpty()
    {
        var previous = BitmapImage.Downloader;
        BitmapImage.Downloader = _ => throw new HttpRequestException("offline");
        try
        {
            var image = new UrlImage { Url = "https://render.albiononline.com/v1/avatar/FAIL" };
            await image.LoadTask;
            AvaloniaDispatcher.UIThread.RunJobs();

            image.Source.Should().BeNull();
        }
        finally
        {
            BitmapImage.Downloader = previous;
        }
    }

    [AvaloniaTest]
    public async Task ALateDownloadOfAnOlderUrlDoesNotOverwriteTheNewerOne()
    {
        var previous = BitmapImage.Downloader;
        var first = new TaskCompletionSource<byte[]>();
        BitmapImage.Downloader = uri => uri.AbsoluteUri.EndsWith("/SLOW") ? first.Task : Task.FromResult(Png);
        try
        {
            var image = new UrlImage { Url = "https://render.albiononline.com/v1/avatar/SLOW" };
            var slow = image.LoadTask;
            image.Url = "https://render.albiononline.com/v1/avatar/FAST";
            await image.LoadTask;
            AvaloniaDispatcher.UIThread.RunJobs();
            var fastSource = image.Source;
            fastSource.Should().NotBeNull();

            first.SetResult(Png);
            await slow;
            AvaloniaDispatcher.UIThread.RunJobs();

            image.Source.Should().BeSameAs(fastSource);
        }
        finally
        {
            BitmapImage.Downloader = previous;
        }
    }
}
