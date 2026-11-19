using StatisticsAnalysisTool.Headless.Contract;

namespace StatisticsAnalysisTool.Headless;

public static class StartupCheck
{
    public const int ExitNoPrivilege = 2;
    private const string Capabilities = "cap_net_raw,cap_net_admin=eip";

    public static string SetcapCommand(string target)
    {
        var quoted = target.IndexOfAny([' ', '\'', '"', '$', '`', '\\', '(', ')', '&', ';', '*', '?']) >= 0
            ? "'" + target.Replace("'", "'\\''") + "'"
            : target;
        return $"sudo setcap {Capabilities} {quoted}";
    }

    /// <summary>Returns 0 when capture is allowed, otherwise emits a no_privilege status and returns the exit code.</summary>
    public static int Run(bool isCapable, string binaryPath, IEventSink sink)
    {
        if (isCapable)
        {
            return 0;
        }

        sink.Emit(new StatusMessage(StatusStates.NoPrivilege,
            "Packet capture needs CAP_NET_RAW (or root). Grant it once with: " + SetcapCommand(binaryPath) + " then start sat-headless again."));
        return ExitNoPrivilege;
    }
}
