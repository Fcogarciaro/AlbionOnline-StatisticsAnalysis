// Linux replacement for Network/PacketProviders/SocketsPacketProvider.cs. The upstream class reads raw IP traffic with
// the Windows-only SIO_RCVALL socket control code, which Linux does not support. The "Sockets" setting (kept in the
// enum and in settings files copied from Windows) therefore runs the libpcap provider instead.
using StatisticsAnalysisTool.Abstractions;

namespace StatisticsAnalysisTool.Network.PacketProviders;

public class SocketsPacketProvider(IPhotonReceiver photonReceiver, AlbionServerDetectionService albionServerDetectionService)
    : LibpcapPacketProvider(photonReceiver, albionServerDetectionService)
{
}
