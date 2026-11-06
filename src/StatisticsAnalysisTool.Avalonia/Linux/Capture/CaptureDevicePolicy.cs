// Device selection rules specific to Linux libpcap.
using System;

namespace StatisticsAnalysisTool.Linux.Capture;

internal static class CaptureDevicePolicy
{
    /// <summary>
    /// The "any" pseudo device captures every interface with Linux cooked headers. Opening it next to the real
    /// interfaces would deliver each packet twice, so it is only a fallback when nothing else could be opened.
    /// </summary>
    public static bool IsAnyDevice(string deviceName) => string.Equals(deviceName, "any", StringComparison.Ordinal);
}
