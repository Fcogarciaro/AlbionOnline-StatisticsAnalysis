// Opens web links in the default browser through xdg-open (the WPF code uses Process.Start with UseShellExecute).
// Only http(s) is opened: the URLs come from settings and localized texts, and file:, javascript: or option-looking
// values must never reach the desktop handler.
using Serilog;
using System;
using System.Diagnostics;
using System.IO;

namespace StatisticsAnalysisTool.Linux.Platform;

public static class ExternalLink
{
    public static bool IsOpenable(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    public static ProcessStartInfo CreateStartInfo(string url)
    {
        var info = new ProcessStartInfo("xdg-open")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        info.ArgumentList.Add(new Uri(url, UriKind.Absolute).AbsoluteUri);
        return info;
    }

    public static bool IsOpenableFolder(string path) => !string.IsNullOrWhiteSpace(path) && Path.IsPathRooted(path);

    public static ProcessStartInfo CreateOpenFolderStartInfo(string path)
    {
        var info = new ProcessStartInfo("xdg-open")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        info.ArgumentList.Add(path);
        return info;
    }

    /// <summary>Opens a folder in the default file manager (WPF: Process.Start with UseShellExecute). False on failure.</summary>
    public static bool OpenFolder(string path)
    {
        if (!IsOpenableFolder(path))
        {
            Log.Warning("Refused to open a folder that is not an absolute path: {Path}", path);
            return false;
        }

        try
        {
            Process.Start(CreateOpenFolderStartInfo(path))?.Dispose();
            return true;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "The folder could not be opened with xdg-open: {Path}", path);
            return false;
        }
    }

    public static bool Open(string url)
    {
        if (!IsOpenable(url))
        {
            Log.Warning("Refused to open a link that is not an http(s) address: {Url}", url);
            return false;
        }

        try
        {
            Process.Start(CreateStartInfo(url))?.Dispose();
            return true;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "The link could not be opened with xdg-open: {Url}", url);
            return false;
        }
    }
}
