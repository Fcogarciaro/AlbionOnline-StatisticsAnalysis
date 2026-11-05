// Taskbar attention request (WPF FlashWindowEx). Avalonia has no API; on X11 the window manager is asked through
// wmctrl to set or clear _NET_WM_STATE_DEMANDS_ATTENTION. Wayland has no equivalent without a focus token.
using System;
using System.Collections.Generic;

namespace StatisticsAnalysisTool.Linux.Platform;

internal static class WindowAttention
{
    /// <summary>wmctrl arguments for the window, or null when unsupported (not an X11 handle or no wmctrl).</summary>
    public static IReadOnlyList<string> BuildWmctrlArguments(string handleDescriptor, IntPtr handle, bool demandAttention, bool wmctrlAvailable)
    {
        if (!wmctrlAvailable || handleDescriptor != "XID" || handle == IntPtr.Zero)
        {
            return null;
        }

        return ["-i", "-r", $"0x{handle.ToInt64():x}", "-b", $"{(demandAttention ? "add" : "remove")},demand_attention"];
    }
}
