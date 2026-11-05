// Finds helper executables (audio players, notify-send) on PATH without spawning anything.
using System;
using System.IO;
using System.Linq;

namespace StatisticsAnalysisTool.Linux.Platform;

internal static class ExecutableLocator
{
    public static bool IsAvailable(string name) => IsAvailable(name, Environment.GetEnvironmentVariable("PATH"));

    public static bool IsAvailable(string name, string pathVariable)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(pathVariable))
        {
            return false;
        }

        const UnixFileMode executable = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        return pathVariable.Split(':', StringSplitOptions.RemoveEmptyEntries)
            .Select(dir => Path.Combine(dir, name))
            .Any(path => File.Exists(path) && (File.GetUnixFileMode(path) & executable) != 0);
    }
}
