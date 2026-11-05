// XDG Base Directory roots for the app (https://specifications.freedesktop.org/basedir-spec/).
// Relative or empty variable values are ignored, as the spec requires.
using System;
using System.IO;

namespace StatisticsAnalysisTool.Linux.Platform;

internal sealed class XdgDirectories
{
    public const string AppFolderName = "StatisticsAnalysisTool";

    private XdgDirectories(string configRoot, string dataRoot, string cacheRoot)
    {
        ConfigRoot = configRoot;
        DataRoot = dataRoot;
        CacheRoot = cacheRoot;
    }

    /// <summary>$XDG_CONFIG_HOME/StatisticsAnalysisTool: settings.</summary>
    public string ConfigRoot { get; }

    /// <summary>$XDG_DATA_HOME/StatisticsAnalysisTool: user data, backups, logs, game files.</summary>
    public string DataRoot { get; }

    /// <summary>$XDG_CACHE_HOME/StatisticsAnalysisTool: temp files and downloaded images (re-downloadable).</summary>
    public string CacheRoot { get; }

    public static XdgDirectories Current { get; private set; } = Resolve(Environment.GetEnvironmentVariable);

    public static XdgDirectories Resolve(Func<string, string> getEnvironmentVariable)
    {
        var home = getEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
        {
            home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return new XdgDirectories(
            Path.Combine(Home(getEnvironmentVariable, "XDG_CONFIG_HOME", home, ".config"), AppFolderName),
            Path.Combine(Home(getEnvironmentVariable, "XDG_DATA_HOME", home, ".local/share"), AppFolderName),
            Path.Combine(Home(getEnvironmentVariable, "XDG_CACHE_HOME", home, ".cache"), AppFolderName));
    }

    internal static IDisposable UseForTests(XdgDirectories directories)
    {
        var previous = Current;
        Current = directories;
        return new Restore(previous);
    }

    private static string Home(Func<string, string> getEnvironmentVariable, string variable, string home, string fallback)
    {
        var value = getEnvironmentVariable(variable);
        return !string.IsNullOrEmpty(value) && Path.IsPathRooted(value) ? value : Path.Combine(home, fallback);
    }

    private sealed class Restore(XdgDirectories previous) : IDisposable
    {
        public void Dispose() => Current = previous;
    }
}
