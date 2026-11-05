// Detects the Albion Online client on Linux. Process.GetProcessesByName only sees the kernel "comm" name, which is
// truncated to 15 characters and, under Wine/Proton, is the Windows exe ("Albion-Online.e") or a wine loader.
// This matches comm and argv[0] of every /proc entry instead.
using System;
using System.Collections.Generic;
using System.IO;

namespace StatisticsAnalysisTool.Linux.Platform;

internal static class AlbionProcessMatcher
{
    public const string ProcessName = "Albion-Online";

    /// <summary>True when the kernel comm name (possibly truncated to 15 chars) or argv[0]'s file name is the game.</summary>
    public static bool IsMatch(string comm, string argv0)
    {
        const string windowsExe = ProcessName + ".exe";

        comm = comm?.Trim();
        // A (possibly truncated) prefix of "Albion-Online.exe" that already spells the game name.
        if (!string.IsNullOrEmpty(comm) && comm.StartsWith(ProcessName, StringComparison.OrdinalIgnoreCase)
                                        && windowsExe.StartsWith(comm, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(argv0))
        {
            return false;
        }

        // Wine reports Windows paths with backslashes; take the file name after either separator.
        var fileName = argv0.Substring(argv0.LastIndexOfAny(['/', '\\']) + 1);
        return fileName.Equals(ProcessName, StringComparison.OrdinalIgnoreCase)
               || fileName.Equals(windowsExe, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Scans a /proc-like directory; unreadable or vanishing entries are skipped.</summary>
    public static bool IsGameRunning(string procRoot)
    {
        IEnumerable<string> directories;
        try
        {
            directories = Directory.Exists(procRoot) ? Directory.EnumerateDirectories(procRoot) : [];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        foreach (var directory in directories)
        {
            if (!int.TryParse(Path.GetFileName(directory), out _))
            {
                continue;
            }

            try
            {
                var comm = ReadOrEmpty(Path.Combine(directory, "comm"));
                var cmdline = ReadOrEmpty(Path.Combine(directory, "cmdline"));
                if (IsMatch(comm, cmdline.Split('\0')[0]))
                {
                    return true;
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // The process exited or belongs to another user; keep scanning.
            }
        }

        return false;
    }

    private static string ReadOrEmpty(string path) => File.Exists(path) ? File.ReadAllText(path) : string.Empty;
}
