// Linux version of ShowNpcapInfoDialogOnFirstStart's content: the Npcap link becomes the capabilities(7) man page and,
// when the process lacks CAP_NET_RAW, the dialog carries the exact setcap command even when libpcap lists devices.
using StatisticsAnalysisTool.Linux.Capture;

namespace StatisticsAnalysisTool.Linux.Startup;

internal static class CaptureStartupDialog
{
    public const string InfoUrl = "https://man7.org/linux/man-pages/man7/capabilities.7.html";

    public static (string Title, string Text) Build(string title, string message, string linkText, CaptureEnvironment environment)
    {
        var permission = environment.IsCapable
            ? null
            : CaptureGuidance.PermissionMessage(environment.ProcessPath, environment.EntryAssemblyPath);
        var body = permission == null || message.Contains(permission, System.StringComparison.Ordinal)
            ? message
            : $"{message}\n\n{permission}";
        return (title, $"{body}\n\n{linkText}: {InfoUrl}");
    }
}
