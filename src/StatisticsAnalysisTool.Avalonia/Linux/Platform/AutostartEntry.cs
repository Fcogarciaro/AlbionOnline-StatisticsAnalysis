// XDG autostart (https://specifications.freedesktop.org/autostart-spec/): a .desktop file in
// $XDG_CONFIG_HOME/autostart (default ~/.config/autostart) replaces the Windows HKCU Run registry value.
using System;
using Serilog;
using System.IO;
using System.Text;

namespace StatisticsAnalysisTool.Linux.Platform;

internal static class AutostartEntry
{
    public const string FileName = "StatisticsAnalysisTool.desktop";

    /// <summary>XDG_CONFIG_HOME when it is an absolute path, otherwise $HOME/.config; then "autostart".</summary>
    public static string ResolveDirectory(Func<string, string> getEnvironmentVariable)
    {
        var xdg = getEnvironmentVariable("XDG_CONFIG_HOME");
        var configHome = !string.IsNullOrEmpty(xdg) && Path.IsPathRooted(xdg)
            ? xdg
            : Path.Combine(getEnvironmentVariable("HOME") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        return Path.Combine(configHome, "autostart");
    }

    /// <summary>Exec value: the executable, or "dotnet &lt;dll&gt;" when the process was started through the dotnet host.</summary>
    public static string BuildExec(string processPath, string entryAssemblyPath)
    {
        var isDotnetHost = string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.Ordinal);
        return isDotnetHost && !string.IsNullOrEmpty(entryAssemblyPath)
            ? $"{QuoteExecArgument(processPath)} {QuoteExecArgument(entryAssemblyPath)}"
            : QuoteExecArgument(processPath);
    }

    /// <summary>Quotes one Exec argument per the Desktop Entry spec (double quotes, escapes, %% for percent).</summary>
    public static string QuoteExecArgument(string argument)
    {
        var builder = new StringBuilder("\"");
        foreach (var c in argument)
        {
            if (c == '\\')
            {
                // Two unescape passes (string value, then Exec quoting): one backslash is written as four.
                builder.Append("\\\\\\\\");
                continue;
            }

            if (c is '"' or '$' or '`')
            {
                builder.Append("\\\\");
            }
            else if (c == '%')
            {
                builder.Append('%');
            }

            builder.Append(c);
        }

        return builder.Append('"').ToString();
    }

    /// <summary>Escapes a string value per the Desktop Entry spec (backslash and control characters).</summary>
    public static string EscapeValue(string value) =>
        value.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");

    public static string BuildContent(string exec, string workingDirectory = null) =>
        "[Desktop Entry]\n"
        + "Type=Application\n"
        + "Name=Statistics Analysis Tool\n"
        + "Comment=Albion Online Statistics Analysis Tool\n"
        + $"Exec={exec}\n"
        + (string.IsNullOrEmpty(workingDirectory) ? string.Empty : $"Path={EscapeValue(workingDirectory)}\n")
        + "Terminal=false\n"
        + "X-GNOME-Autostart-enabled=true\n";

    public static bool TrySet(bool isEnabled, string directory, string exec, string workingDirectory = null)
    {
        try
        {
            var path = Path.Combine(directory, FileName);
            if (isEnabled)
            {
                Directory.CreateDirectory(directory);
                var temporaryPath = path + ".tmp";
                File.WriteAllText(temporaryPath, BuildContent(exec, workingDirectory));
                File.Move(temporaryPath, path, true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }

            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Log.Error(e, "The autostart entry could not be updated");
            return false;
        }
    }
}
