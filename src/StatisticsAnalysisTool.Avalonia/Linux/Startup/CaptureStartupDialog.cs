// Linux version of ShowNpcapInfoDialogOnFirstStart's content: the Npcap link becomes the capabilities(7) man page and,
// when the process lacks CAP_NET_RAW, the dialog carries the exact setcap command even when libpcap lists devices.
using StatisticsAnalysisTool.Linux.Capture;

namespace StatisticsAnalysisTool.Linux.Startup;

internal static class CaptureStartupDialog
{
    public const string InfoUrl = "https://man7.org/linux/man-pages/man7/capabilities.7.html";

    /// <summary>Content of the DialogWindow shown on the first start: the link is a separate clickable element.</summary>
    public static CaptureDialogContent BuildContent(string title, string message, string linkText, CaptureEnvironment environment)
    {
        var permission = environment.IsCapable
            ? null
            : CaptureGuidance.PermissionMessage(environment.ProcessPath, environment.EntryAssemblyPath);
        var body = permission == null || message.Contains(permission, System.StringComparison.Ordinal)
            ? message
            : $"{message}\n\n{permission}";
        return new CaptureDialogContent(title, body, linkText, InfoUrl);
    }

    /// <summary>Plain text form (message box): the link is appended to the message.</summary>
    public static (string Title, string Text) Build(string title, string message, string linkText, CaptureEnvironment environment)
    {
        var content = BuildContent(title, message, linkText, environment);
        return (content.Title, $"{content.Message}\n\n{linkText}: {content.Url}");
    }
}

internal sealed record CaptureDialogContent(string Title, string Message, string LinkText, string Url);
