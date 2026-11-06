// Linux replacement for Core/ApplicationCore.cs. The upstream check uses WindowsPrincipal, which throws
// PlatformNotSupportedException on Linux. Upstream only asks "may this process capture packets?", which on Linux
// means root or CAP_NET_RAW.
using StatisticsAnalysisTool.Linux.Capture;

namespace StatisticsAnalysisTool.Core;

public class ApplicationCore
{
    public static bool IsAppStartedAsAdministrator()
    {
        return CapturePrivilege.IsCurrentProcessCapable();
    }
}
