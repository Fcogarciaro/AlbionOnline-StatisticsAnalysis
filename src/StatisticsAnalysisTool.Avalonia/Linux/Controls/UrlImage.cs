// WPF converts a url string bound to Image.Source into a BitmapImage that downloads asynchronously; Avalonia does not convert
// strings in bindings. UrlImage is an Image with a Url property that does it: only http(s) urls load, a failed download leaves
// the image empty, and a late result of an older url never replaces the current one.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace StatisticsAnalysisTool.Common.Controls;

public class UrlImage : Image
{
    public static readonly StyledProperty<string> UrlProperty =
        AvaloniaProperty.Register<UrlImage, string>(nameof(Url));

    private int _generation;

    static UrlImage()
    {
        UrlProperty.Changed.AddClassHandler<UrlImage>((image, _) => image.UrlChanged());
    }

    protected override Type StyleKeyOverride => typeof(Image);

    public string Url
    {
        get => GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    /// <summary>Completes when the load started by the last url change has finished (successfully, failed or superseded).</summary>
    public Task LoadTask { get; private set; } = Task.CompletedTask;

    public static bool TryGetWebUri(string url, out Uri uri)
    {
        if (Uri.TryCreate(url?.Trim(), UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
        {
            return true;
        }

        uri = null;
        return false;
    }

    private void UrlChanged()
    {
        var generation = ++_generation;
        Source = null;
        if (!TryGetWebUri(Url, out var uri))
        {
            LoadTask = Task.CompletedTask;
            return;
        }

        LoadTask = LoadAsync(uri, generation);
    }

    private async Task LoadAsync(Uri uri, int generation)
    {
        Bitmap bitmap = null;
        try
        {
            var bytes = await BitmapImage.Downloader(uri).ConfigureAwait(true);
            using var stream = new MemoryStream(bytes);
            bitmap = new Bitmap(stream);
        }
        catch (Exception)
        {
            // An unreachable server or a corrupt image leaves the image empty, like WPF.
        }

        if (generation == _generation)
        {
            Source = bitmap;
        }
    }
}
