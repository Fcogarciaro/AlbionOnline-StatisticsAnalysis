// Finds the Albion Online main game folder (the one that contains "game", see Extractor.IsValidMainGameFolder) on Linux.
// There is no registry or "C:\Program Files" default here, so the usual places are probed instead:
//  - Steam: <steam>/steamapps/common/Albion Online for the Steam homes (native, ~/.steam, Flatpak) and every library
//    folder named in steamapps/libraryfolders.vdf;
//  - the standalone launcher under Wine: <prefix>/drive_c/Program Files (x86)/AlbionOnline for WINEPREFIX, ~/.wine,
//    Lutris/PlayOnLinux style ~/Games/* prefixes and Bottles (native and Flatpak).
// Nothing is created or modified; unreadable locations are skipped.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StatisticsAnalysisTool.Linux.Platform;

public static class GameFolderLocator
{
    private const string SteamGameFolder = "Albion Online";
    private const string LauncherGameFolder = "AlbionOnline";

    private static readonly Regex VdfPath = new("^\\s*\"path\"\\s+\"(?<path>[^\"]+)\"", RegexOptions.Multiline | RegexOptions.Compiled);

    public static IEnumerable<string> Candidates(string home, Func<string, string> getEnvironmentVariable)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in EnumerateCandidates(home, getEnvironmentVariable))
        {
            if (seen.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    public static IEnumerable<string> FindValid(string home, Func<string, string> getEnvironmentVariable, Func<string, bool> isValidMainGameFolder)
    {
        foreach (var candidate in Candidates(home, getEnvironmentVariable))
        {
            if (IsValid(candidate, isValidMainGameFolder))
            {
                yield return candidate;
            }
        }
    }

    /// <summary>Where the folder picker should open: the deepest existing parent of the first plausible location, else home.</summary>
    public static string SuggestedStart(string home, Func<string, string> getEnvironmentVariable)
    {
        foreach (var candidate in Candidates(home, getEnvironmentVariable))
        {
            var parent = Path.GetDirectoryName(candidate);
            while (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
            {
                parent = Path.GetDirectoryName(parent);
            }

            if (!string.IsNullOrEmpty(parent) && !string.Equals(parent, home, StringComparison.Ordinal)
                                              && parent.StartsWith(home, StringComparison.Ordinal))
            {
                return parent;
            }
        }

        return home;
    }

    private static bool IsValid(string folder, Func<string, bool> isValidMainGameFolder)
    {
        try
        {
            return Directory.Exists(folder) && isValidMainGameFolder(folder);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    private static IEnumerable<string> EnumerateCandidates(string home, Func<string, string> getEnvironmentVariable)
    {
        var dataHome = getEnvironmentVariable("XDG_DATA_HOME");
        dataHome = !string.IsNullOrWhiteSpace(dataHome) && Path.IsPathRooted(dataHome) ? dataHome : Path.Combine(home, ".local/share");

        var steamHomes = new List<string>
        {
            Path.Combine(dataHome, "Steam"),
            Path.Combine(home, ".local/share/Steam"),
            Path.Combine(home, ".steam/steam"),
            Path.Combine(home, ".steam/root"),
            Path.Combine(home, ".var/app/com.valvesoftware.Steam/.local/share/Steam"),
            Path.Combine(home, ".var/app/com.valvesoftware.Steam/data/Steam")
        };

        // The documented order starts with the usual locations; library folders follow their Steam home.
        foreach (var steamHome in steamHomes)
        {
            yield return Path.Combine(steamHome, "steamapps/common", SteamGameFolder);
        }

        foreach (var steamHome in steamHomes)
        {
            foreach (var library in ReadSteamLibraries(Path.Combine(steamHome, "steamapps/libraryfolders.vdf")))
            {
                yield return Path.Combine(library, "steamapps/common", SteamGameFolder);
            }
        }

        foreach (var prefix in WinePrefixes(home, dataHome, getEnvironmentVariable))
        {
            yield return Path.Combine(prefix, "drive_c/Program Files (x86)", LauncherGameFolder);
            yield return Path.Combine(prefix, "drive_c/Program Files", LauncherGameFolder);
        }
    }

    private static IEnumerable<string> ReadSteamLibraries(string vdfFile)
    {
        string text;
        try
        {
            if (!File.Exists(vdfFile))
            {
                yield break;
            }

            text = File.ReadAllText(vdfFile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (Match match in VdfPath.Matches(text))
        {
            var path = match.Groups["path"].Value.Replace("\\\\", "\\");
            if (Path.IsPathRooted(path))
            {
                yield return path;
            }
        }
    }

    private static IEnumerable<string> WinePrefixes(string home, string dataHome, Func<string, string> getEnvironmentVariable)
    {
        var winePrefix = getEnvironmentVariable("WINEPREFIX");
        if (!string.IsNullOrWhiteSpace(winePrefix) && Path.IsPathRooted(winePrefix))
        {
            yield return winePrefix;
        }

        yield return Path.Combine(home, ".wine");

        foreach (var prefix in PrefixesBelow(Path.Combine(home, "Games")))
        {
            yield return prefix;
        }

        foreach (var bottles in new[]
                 {
                     Path.Combine(dataHome, "bottles/bottles"),
                     Path.Combine(home, ".local/share/bottles/bottles"),
                     Path.Combine(home, ".var/app/com.usebottles.bottles/data/bottles/bottles")
                 })
        {
            foreach (var prefix in PrefixesBelow(bottles))
            {
                yield return prefix;
            }
        }
    }

    private static IEnumerable<string> PrefixesBelow(string folder)
    {
        string[] children;
        try
        {
            if (!Directory.Exists(folder))
            {
                return [];
            }

            children = Directory.GetDirectories(folder);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        return children.Where(child => Directory.Exists(Path.Combine(child, "drive_c"))).OrderBy(child => child, StringComparer.Ordinal);
    }
}
