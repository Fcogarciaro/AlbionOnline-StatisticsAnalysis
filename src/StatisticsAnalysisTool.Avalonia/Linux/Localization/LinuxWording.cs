// Linux wording for settings and error texts that the localization table words for Windows ("Start with Windows",
// "notification area", Administrator, Npcap). Hooked into LocalizationController.Translation next to CaptureGuidance.
// English only, like the capture texts: the table has no Linux strings, and a translated "Windows" would be wrong.
using System.Collections.Generic;

namespace StatisticsAnalysisTool.Linux.Localization;

internal static class LinuxWording
{
    private static readonly Dictionary<string, string> Texts = new()
    {
        ["START_WITH_WINDOWS"] = "Start at login",
        ["START_WITH_WINDOWS_DESCRIPTION"] = "Starts Statistics Analysis Tool automatically when you log in to your desktop session (XDG autostart entry).",
        ["START_IN_SYSTEM_TRAY"] = "Start in the system tray",
        ["START_IN_SYSTEM_TRAY_DESCRIPTION"] = "Hides the window in the system tray immediately after launch. Needs a tray host (StatusNotifierItem), for example the waybar tray.",
        ["OPEN_WITH_GAME_DESCRIPTION"] = "Restores the window from the system tray when Albion Online starts.",
        ["HIDE_WITH_GAME_DESCRIPTION"] = "Hides the window in the system tray when Albion Online closes.",
        ["MINIMIZE_TO_SYSTEM_TRAY"] = "Hide in the system tray when minimized",
        ["APPLICATION_CONTINUES_TO_RUN_IN_SYSTEM_TRAY"] = "Statistics Analysis Tool continues to run in the system tray.",
        ["WINDOWS_STARTUP_SETTING_UPDATE_FAILED"] = "The autostart entry could not be updated. Please try again.",
        ["CREATE_DESKTOP_SHORTCUT"] = "Create desktop entry",
        ["TOOL_MUST_BE_RUN_AS_ADMIN"] = "needs capture permission",
        ["ERR_SOCKET_FAILED_WITH_CODE"] = "Packet capture could not start (code: {0}). Check that the tool may capture packets (setcap or root) and that the selected adapter is up.",
        ["PACKET_HANDLER_ERROR_MESSAGE"] = "Tracking could not be started. Please verify that libpcap is installed and that the tool may capture packets (setcap or root)."
    };

    public static bool TryGetTextOverride(string key, out string text)
    {
        text = null;
        return key != null && Texts.TryGetValue(key, out text);
    }
}
