// Sends a freedesktop desktop notification through notify-send (fire and forget).
using Serilog;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace StatisticsAnalysisTool.Linux.Platform;

internal static class DesktopNotifier
{
    public static void Send(NotificationKind kind, string title, string message)
    {
        try
        {
            var startInfo = new ProcessStartInfo("notify-send") { UseShellExecute = false, CreateNoWindow = true };
            foreach (var argument in NotificationPlan.NotifySendArguments(kind, title, message))
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo);
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException)
        {
            Log.Warning(e, "notify-send failed");
        }
    }
}
