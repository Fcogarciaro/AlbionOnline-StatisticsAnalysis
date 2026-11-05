// Replaces the Npcap/administrator checks: raw packet capture on Linux needs root, or the CAP_NET_RAW and
// CAP_NET_ADMIN capabilities in the effective set of the process (granted to the binary with setcap).
using System;
using System.IO;

namespace StatisticsAnalysisTool.Linux.Capture;

internal static class CapturePrivilege
{
    private const int CapNetAdmin = 12;
    private const int CapNetRaw = 13;
    private const ulong CaptureMask = (1UL << CapNetAdmin) | (1UL << CapNetRaw);

    /// <summary>Reads the hex mask of the "CapEff:" line of /proc/self/status, or null when absent or malformed.</summary>
    public static ulong? ParseCapEff(string procStatus)
    {
        if (string.IsNullOrEmpty(procStatus))
        {
            return null;
        }

        foreach (var line in procStatus.Split('\n'))
        {
            if (!line.StartsWith("CapEff:", StringComparison.Ordinal))
            {
                continue;
            }

            var value = line["CapEff:".Length..].Trim();
            return ulong.TryParse(value, System.Globalization.NumberStyles.AllowHexSpecifier, null, out var mask) ? mask : null;
        }

        return null;
    }

    public static bool HasCaptureCapabilities(ulong capEff) => (capEff & CaptureMask) == CaptureMask;

    public static bool IsCapable(bool isRoot, string procStatus)
    {
        if (isRoot)
        {
            return true;
        }

        var capEff = ParseCapEff(procStatus);
        return capEff.HasValue && HasCaptureCapabilities(capEff.Value);
    }

    public static bool IsCurrentProcessCapable()
    {
        string status = null;
        try
        {
            status = File.ReadAllText("/proc/self/status");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Not readable: only root is accepted.
        }

        return IsCapable(Environment.IsPrivilegedProcess, status);
    }
}
