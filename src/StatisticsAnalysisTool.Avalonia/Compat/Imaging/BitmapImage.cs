// Compatibility shim: System.Windows.Media.Imaging.BitmapImage over Avalonia's Bitmap.
// Supports the BeginInit/EndInit protocol, UriSource (file, avares, pack:// mapped to avares://, http/https
// with async download events) and StreamSource, decode sizes, CacheOption/CreateOptions, and Freeze.
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace System.Windows.Media.Imaging
{
    public enum BitmapCacheOption { Default = 0, OnDemand = 0, OnLoad = 1, None = 2 }

    [Flags]
    public enum BitmapCreateOptions
    {
        None = 0,
        PreservePixelFormat = 1,
        DelayCreation = 2,
        IgnoreColorProfile = 4,
        IgnoreImageCache = 8
    }

    public sealed class BitmapImage : BitmapSource
    {
        private static readonly HttpClient Http = CreateHttpClient();

        private Uri _uriSource;
        private IO.Stream _streamSource;
        private BitmapCacheOption _cacheOption;
        private BitmapCreateOptions _createOptions;
        private int _decodePixelWidth;
        private int _decodePixelHeight;
        private bool _initializing;
        private bool _initialized;
        private volatile bool _downloading;
        private volatile Bitmap _bitmap;

        public BitmapImage()
        {
        }

        public BitmapImage(Uri uriSource)
        {
            BeginInit();
            UriSource = uriSource;
            EndInit();
        }

        /// <summary>Test seam: downloads the bytes of an http(s) image.</summary>
        internal static Func<Uri, Task<byte[]>> Downloader { get; set; } = uri => Http.GetByteArrayAsync(uri);

        /// <summary>Completes when the (possibly asynchronous) load triggered by EndInit has finished.</summary>
        internal Task LoadTask { get; private set; } = Task.CompletedTask;

        public override Bitmap AvaloniaBitmap => _bitmap;

        public override bool IsDownloading => _downloading;

        public override bool CanFreeze => !_downloading && !(_initializing && !_initialized);

        public bool IsInitialized => _initialized;

        public Uri UriSource
        {
            get => _uriSource;
            set { GuardSet(); _uriSource = value; }
        }

        public IO.Stream StreamSource
        {
            get => _streamSource;
            set { GuardSet(); _streamSource = value; }
        }

        public BitmapCacheOption CacheOption
        {
            get => _cacheOption;
            set { GuardSet(); _cacheOption = value; }
        }

        public BitmapCreateOptions CreateOptions
        {
            get => _createOptions;
            set { GuardSet(); _createOptions = value; }
        }

        public int DecodePixelWidth
        {
            get => _decodePixelWidth;
            set { GuardSet(); _decodePixelWidth = value; }
        }

        public int DecodePixelHeight
        {
            get => _decodePixelHeight;
            set { GuardSet(); _decodePixelHeight = value; }
        }

        public void BeginInit()
        {
            if (IsFrozen || _initialized || _initializing)
            {
                throw new InvalidOperationException("BeginInit cannot be called on a frozen, initializing or initialized image.");
            }

            _initializing = true;
        }

        public void EndInit()
        {
            if (!_initializing)
            {
                throw new InvalidOperationException("EndInit must be preceded by BeginInit.");
            }

            if (_uriSource == null && _streamSource == null)
            {
                throw new InvalidOperationException("Either UriSource or StreamSource must be set before EndInit.");
            }

            _initializing = false;
            _initialized = true;

            if (_streamSource != null)
            {
                _bitmap = Decode(_streamSource);
                return;
            }

            var uri = MapPackUri(_uriSource);
            if (uri.Scheme is "http" or "https")
            {
                _downloading = true;
                LoadTask = DownloadAsync(uri);
                return;
            }

            using var stream = OpenLocal(uri);
            _bitmap = Decode(stream);
        }

        /// <summary>Maps WPF pack://application URIs to the Avalonia avares:// equivalent; other URIs are unchanged.</summary>
        public static Uri MapPackUri(Uri uri)
        {
            if (uri is not { IsAbsoluteUri: true } || !uri.Scheme.Equals("pack", StringComparison.OrdinalIgnoreCase))
            {
                return uri;
            }

            // "pack://application:,,,/Assembly;component/a/b.png": drop the scheme and the authority segment.
            var withoutScheme = Uri.UnescapeDataString(uri.AbsoluteUri)["pack://".Length..];
            var slash = withoutScheme.IndexOf('/');
            var path = slash >= 0 ? withoutScheme[(slash + 1)..] : string.Empty;
            var componentIndex = path.IndexOf(";component/", StringComparison.OrdinalIgnoreCase);
            if (componentIndex >= 0)
            {
                var assembly = path[..componentIndex];
                var resource = path[(componentIndex + ";component/".Length)..];
                return new Uri($"avares://{assembly}/{resource}");
            }

            return new Uri($"avares://{DefaultAssemblyName}/{path}");
        }

        private static string DefaultAssemblyName => typeof(BitmapImage).Assembly.GetName().Name;

        private static IO.Stream OpenLocal(Uri uri) => uri.Scheme switch
        {
            "avares" => AssetLoader.Open(uri),
            _ => new IO.FileStream(uri.IsAbsoluteUri ? uri.LocalPath : uri.OriginalString, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
        };

        private Bitmap Decode(IO.Stream stream)
        {
            if (_decodePixelWidth > 0 && _decodePixelHeight > 0)
            {
                using var full = new Bitmap(stream);
                return full.CreateScaledBitmap(new Avalonia.PixelSize(_decodePixelWidth, _decodePixelHeight));
            }

            if (_decodePixelWidth > 0)
            {
                return Bitmap.DecodeToWidth(stream, _decodePixelWidth);
            }

            return _decodePixelHeight > 0 ? Bitmap.DecodeToHeight(stream, _decodePixelHeight) : new Bitmap(stream);
        }

        private async Task DownloadAsync(Uri uri)
        {
            try
            {
                var bytes = await Downloader(uri).ConfigureAwait(false);
                using var memory = new IO.MemoryStream(bytes);
                var bitmap = Decode(memory);
                await Complete(bitmap, null).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Complete(null, ex).ConfigureAwait(false);
            }
        }

        // The returned task completes only after Finish ran on the UI thread, so LoadTask implies a published bitmap.
        private Task Complete(Bitmap bitmap, Exception error)
        {
            var done = new TaskCompletionSource();

            void Finish()
            {
                try
                {
                    _bitmap = bitmap;
                    _downloading = false;
                    if (error == null)
                    {
                        RaiseDownloadCompleted();
                    }
                    else
                    {
                        RaiseDownloadFailed(error);
                    }
                }
                finally
                {
                    done.TrySetResult();
                }
            }

            // WPF raises download events asynchronously on the UI dispatcher, never inline from EndInit.
            Dispatcher.UIThread.Post(Finish);
            return done.Task;
        }

        private void GuardSet()
        {
            if (IsFrozen || _initialized)
            {
                throw new InvalidOperationException("Cannot set a property after the image has been initialized or frozen.");
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("StatisticsAnalysisTool-Linux");
            return client;
        }
    }
}
