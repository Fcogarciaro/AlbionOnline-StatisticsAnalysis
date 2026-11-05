// "Create desktop shortcut" on Linux (the Windows build writes a .lnk through Shortcut/ShortcutController): a Desktop Entry for
// the application menu ($XDG_DATA_HOME/applications) plus, when a Desktop folder already exists, an executable copy there.
using Serilog;
using System;
using System.IO;

namespace StatisticsAnalysisTool.Linux.Platform;

internal static class DesktopShortcut
{
    /// <summary>Same file name as packaging/linux/statisticsanalysistool.desktop, so a user-created entry replaces the package one instead of duplicating it.</summary>
    public const string FileName = "statisticsanalysistool.desktop";

    /// <summary>Icon theme name installed by install.sh and the PKGBUILD under hicolor.</summary>
    public const string ThemedIconName = "statisticsanalysistool";

    public static string BuildContent(string exec, string workingDirectory, string iconPath) =>
        "[Desktop Entry]\n"
        + "Type=Application\n"
        + "Name=Statistics Analysis Tool\n"
        + "Comment=Albion Online Statistics Analysis Tool\n"
        + $"Exec={exec}\n"
        + (string.IsNullOrEmpty(workingDirectory) ? string.Empty : $"Path={AutostartEntry.EscapeValue(workingDirectory)}\n")
        + $"Icon={(string.IsNullOrEmpty(iconPath) ? ThemedIconName : AutostartEntry.EscapeValue(iconPath))}\n"
        + "Terminal=false\n"
        + "Categories=Game;Utility;\n";

    public static string ResolveApplicationsDirectory(Func<string, string> getEnvironmentVariable)
    {
        var xdg = getEnvironmentVariable("XDG_DATA_HOME");
        var dataHome = !string.IsNullOrEmpty(xdg) && Path.IsPathRooted(xdg)
            ? xdg
            : Path.Combine(getEnvironmentVariable("HOME") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        return Path.Combine(dataHome, "applications");
    }

    public static bool TryCreate(string applicationsDirectory, string desktopDirectory, string exec, string workingDirectory, string iconPath)
    {
        try
        {
            var content = BuildContent(exec, workingDirectory, iconPath);
            Directory.CreateDirectory(applicationsDirectory);
            Write(Path.Combine(applicationsDirectory, FileName), content);

            if (!string.IsNullOrEmpty(desktopDirectory) && Directory.Exists(desktopDirectory))
            {
                var desktopEntry = Path.Combine(desktopDirectory, FileName);
                Write(desktopEntry, content);
                File.SetUnixFileMode(desktopEntry, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                                                    | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            }

            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Log.Error(e, "The desktop shortcut could not be created");
            return false;
        }
    }

    private static void Write(string path, string content)
    {
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, content);
        File.Move(temporaryPath, path, true);
    }
}
