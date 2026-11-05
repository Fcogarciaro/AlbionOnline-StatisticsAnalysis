// Linux replacement for Common/WindowsStartupService.cs (HKCU Run registry key): writes or removes an XDG autostart
// entry, $XDG_CONFIG_HOME/autostart/StatisticsAnalysisTool.desktop. Same public API.
using StatisticsAnalysisTool.Linux.Platform;
using System;
using System.Reflection;

namespace StatisticsAnalysisTool.Common;

public static class WindowsStartupService
{
    public static bool TrySetEnabled(bool isEnabled)
    {
        var directory = AutostartEntry.ResolveDirectory(Environment.GetEnvironmentVariable);
        var processPath = Environment.ProcessPath;
        if (isEnabled && string.IsNullOrWhiteSpace(processPath))
        {
            Serilog.Log.Error("The autostart entry could not be created because the executable path is unavailable");
            return false;
        }

        var exec = isEnabled ? AutostartEntry.BuildExec(processPath, Assembly.GetEntryAssembly()?.Location) : string.Empty;
        return AutostartEntry.TrySet(isEnabled, directory, exec);
    }
}
