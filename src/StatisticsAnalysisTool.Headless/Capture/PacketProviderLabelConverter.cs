// CaptureGuidance (linked from the Avalonia project) names the packet provider labels of the GUI converter of the same
// name in a few UI-only texts. The headless build has no GUI, so these constants only keep that file compilable.
namespace StatisticsAnalysisTool.Common.Converters;

internal static class PacketProviderLabelConverter
{
    public const string LibpcapLabel = "libpcap";
    public const string SocketsLabel = "Sockets (libpcap)";
}
