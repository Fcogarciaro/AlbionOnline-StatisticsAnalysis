// User-facing guidance shown where the Windows build talks about Npcap: the exact setcap command for the real
// binary, and the libpcap package hint. Texts are injected through the localization keys the Windows UI already uses.
using System;
using System.IO;

namespace StatisticsAnalysisTool.Linux.Capture;

internal sealed record CaptureEnvironment(bool IsCapable, string ProcessPath, string EntryAssemblyPath)
{
    public static CaptureEnvironment Current => new(
        CapturePrivilege.IsCurrentProcessCapable(),
        Environment.ProcessPath ?? string.Empty,
        System.Reflection.Assembly.GetEntryAssembly()?.Location ?? string.Empty);
}

internal static class CaptureGuidance
{
    public const string LibraryMissingMessage =
        "libpcap is not installed. Install it with your package manager, for example " +
        "'sudo pacman -S libpcap' (Arch), 'sudo apt install libpcap0.8' (Debian/Ubuntu) or 'sudo dnf install libpcap' (Fedora), " +
        "then restart the tool.";

    private const string Capabilities = "cap_net_raw,cap_net_admin=eip";

    private static bool IsDotnetHost(string processPath) =>
        string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.Ordinal);

    /// <summary>
    /// The file that must carry the capabilities. Under "dotnet StatisticsAnalysisTool.dll" the process is the dotnet
    /// host (granting it capabilities would apply to every .NET program), so the app launcher next to the dll is returned.
    /// </summary>
    public static string SetcapTarget(string processPath, string entryAssemblyPath)
    {
        if (IsDotnetHost(processPath) && !string.IsNullOrEmpty(entryAssemblyPath))
        {
            return Path.Combine(Path.GetDirectoryName(entryAssemblyPath) ?? string.Empty, Path.GetFileNameWithoutExtension(entryAssemblyPath));
        }

        return processPath;
    }

    public static string SetcapCommand(string target)
    {
        var quoted = target.IndexOfAny([' ', '\'', '"', '$', '`', '\\', '(', ')', '&', ';', '*', '?']) >= 0
            ? "'" + target.Replace("'", "'\\''") + "'"
            : target;
        return $"sudo setcap {Capabilities} {quoted}";
    }

    public static string PermissionMessage(string processPath, string entryAssemblyPath)
    {
        var target = SetcapTarget(processPath, entryAssemblyPath);
        var message = "Capturing network traffic needs the CAP_NET_RAW capability (or root); CAP_NET_ADMIN is optional but granted too, as Wireshark does. " +
                      $"Grant them once with:\n{SetcapCommand(target)}\nThen start the tool again.";

        if (IsDotnetHost(processPath))
        {
            message += $"\nThe tool is running through the dotnet host. The command above targets the launcher {target}; " +
                       "start the tool with that launcher instead of 'dotnet StatisticsAnalysisTool.dll'.";
        }

        return message;
    }

    public static bool TryGetTextOverride(string key, CaptureEnvironment environment, out string text)
    {
        text = null;
        switch (key)
        {
            case "NO_LISTENING_ADAPTERS":
            case "ERR_NPCAP_OPEN_FAILED":
            case "START_APPLICATION_AS_ADMINISTRATOR":
                if (environment.IsCapable)
                {
                    return false;
                }

                text = PermissionMessage(environment.ProcessPath, environment.EntryAssemblyPath);
                return true;

            case "NPCAP_INFO_DIALOG_TITLE":
                text = "Packet capture on Linux";
                return true;

            case "NPCAP_INFO_DIALOG_MESSAGE":
                text = environment.IsCapable
                    ? "Packet capture is ready: the tool has the permissions it needs and uses libpcap."
                    : PermissionMessage(environment.ProcessPath, environment.EntryAssemblyPath);
                return true;

            case "NPCAP_INFO_DIALOG_LINK_TEXT":
                text = "About Linux capabilities";
                return true;

            case "ERR_NPCAP_DLL_MISSING":
                text = LibraryMissingMessage;
                return true;

            case "MAKE_SURE_YOU_HAVE_INSTALLED_NPCAP":
                text = "Make sure libpcap is installed and the tool may capture packets (setcap or root).";
                return true;

            // Info window steps 1 and 2 (Windows 10 and the Npcap installer); the link after step 2 is the man page.
            case "DESCRIPTION_TRACK_MY_GAME_DETAILS_1":
                text = "1. You need Linux with libpcap installed (use your package manager)!";
                return true;

            case "DESCRIPTION_TRACK_MY_GAME_DETAILS_2":
                text = "2. Allow the tool to capture packets once (setcap or root), see: ";
                return true;

            default:
                return false;
        }
    }

    public static bool TryGetTextOverride(string key, out string text) =>
        TryGetTextOverride(key, CaptureEnvironment.Current, out text);
}
