// Linux replacement for Common/SystemInfo.cs. The WPF version uses WMI (System.Management) and
// WindowsPrincipal. This one reads /proc/cpuinfo and treats "admin" as running as root.
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace StatisticsAnalysisTool.Common;

public class SystemInfo
{
    public static void LogSystemInfo()
    {
        try
        {
            Log.Information("OS Version: {OSVersion} - {OSArchitecture} | Processor: {Processor} | Video: {VideoController} | Is Admin: {IsAdmin} | Is Debug: {IsDebug} | Active Networks: {ActiveNetworks}",
                RuntimeInformation.OSDescription, RuntimeInformation.OSArchitecture, GetProcessorName(), GetVideoControllerName(),
                IsRoot(), Debugger.IsAttached, GetActiveNetworkNames());
        }
        catch (Exception ex)
        {
            Log.Error("An unexpected error occurred in LogSystemInfo: {Error}", ex.Message);
        }
    }

    public static string CreateReport()
    {
        var textBuilder = new StringBuilder();

        textBuilder.AppendLine("System data:");
        textBuilder.AppendLine($"OS version: {RuntimeInformation.OSDescription}");
        textBuilder.AppendLine($"OS architecture: {RuntimeInformation.OSArchitecture}");
        textBuilder.AppendLine($"Process architecture: {RuntimeInformation.ProcessArchitecture}");
        textBuilder.AppendLine($"Framework: {RuntimeInformation.FrameworkDescription}");
        textBuilder.AppendLine($"Processor count: {Environment.ProcessorCount}");
        textBuilder.AppendLine($"Processor: {GetProcessorName()}");
        textBuilder.AppendLine($"Video: {GetVideoControllerName()}");
        textBuilder.AppendLine($"Is admin: {IsRoot()}");
        textBuilder.AppendLine($"Is debug: {Debugger.IsAttached}");
        textBuilder.AppendLine($"Active networks: {GetActiveNetworkNames()}");

        return textBuilder.ToString();
    }

    internal static string ParseProcessorName(string cpuInfo)
    {
        var line = cpuInfo?.Split('\n').FirstOrDefault(l => l.StartsWith("model name", StringComparison.OrdinalIgnoreCase));
        var value = line?.Split(':', 2).ElementAtOrDefault(1)?.Trim();
        return string.IsNullOrEmpty(value) ? "Unknown" : value;
    }

    private static bool IsRoot() => Environment.UserName == "root";

    private static string GetProcessorName()
    {
        try
        {
            return ParseProcessorName(File.ReadAllText("/proc/cpuinfo"));
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to retrieve processor info: {Error}", ex.Message);
            return "Unknown";
        }
    }

    // TODO(T03): resolve the GPU name from /sys/class/drm without spawning external tools.
    private static string GetVideoControllerName() => "Unknown";

    private static string GetActiveNetworkNames()
    {
        return string.Join(", ", NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
            .Select(nic => nic.Name));
    }
}
