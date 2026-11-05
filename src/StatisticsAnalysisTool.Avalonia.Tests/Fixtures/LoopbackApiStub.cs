using System.Collections.Concurrent;
using System.Net;
using StatisticsAnalysisTool.Common.UserSettings;

namespace StatisticsAnalysisTool.Avalonia.Tests;

/// <summary>
/// Loopback HTTP server that answers every request with an empty JSON array and records the request paths.
/// While alive, every Albion API base URL in the current settings points at it, so view tests never reach the real network.
/// The original URLs are restored on dispose.
/// </summary>
internal sealed class LoopbackApiStub : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly ConcurrentQueue<string> _requests = new();
    private readonly Task _loop;
    private readonly string[] _originalUrls;

    public LoopbackApiStub()
    {
        var port = FreePort();
        BaseUrl = $"http://127.0.0.1:{port}/";
        _listener.Prefixes.Add(BaseUrl);
        _listener.Start();
        _loop = Task.Run(ServeAsync);

        var settings = SettingsController.CurrentSettings;
        _originalUrls =
        [
            settings.AlbionDataProjectBaseUrlWest, settings.AlbionDataProjectBaseUrlEast, settings.AlbionDataProjectBaseUrlEurope,
            settings.AlbionOnlineApiBaseUrlWest, settings.AlbionOnlineApiBaseUrlEast, settings.AlbionOnlineApiBaseUrlEurope
        ];
        settings.AlbionDataProjectBaseUrlWest = settings.AlbionDataProjectBaseUrlEast = settings.AlbionDataProjectBaseUrlEurope = BaseUrl;
        settings.AlbionOnlineApiBaseUrlWest = settings.AlbionOnlineApiBaseUrlEast = settings.AlbionOnlineApiBaseUrlEurope = BaseUrl.TrimEnd('/');
    }

    public string BaseUrl { get; }

    public IReadOnlyCollection<string> Requests => _requests;

    /// <summary>Waits until at least <paramref name="count"/> requests arrived.</summary>
    public bool WaitForRequests(int count, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (_requests.Count < count && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(10);
        }

        return _requests.Count >= count;
    }

    public void Dispose()
    {
        var settings = SettingsController.CurrentSettings;
        settings.AlbionDataProjectBaseUrlWest = _originalUrls[0];
        settings.AlbionDataProjectBaseUrlEast = _originalUrls[1];
        settings.AlbionDataProjectBaseUrlEurope = _originalUrls[2];
        settings.AlbionOnlineApiBaseUrlWest = _originalUrls[3];
        settings.AlbionOnlineApiBaseUrlEast = _originalUrls[4];
        settings.AlbionOnlineApiBaseUrlEurope = _originalUrls[5];
        _listener.Close();
        try
        {
            _loop.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // The listener was closed while a request was pending.
        }
    }

    private async Task ServeAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception e) when (e is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                return;
            }

            _requests.Enqueue(context.Request.Url!.AbsolutePath);
            var body = "[]"u8.ToArray();
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = body.Length;
            await context.Response.OutputStream.WriteAsync(body);
            context.Response.Close();
        }
    }

    private static int FreePort()
    {
        var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint) probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}
