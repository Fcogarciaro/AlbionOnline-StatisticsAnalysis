// Strips the link-layer header of a captured frame and reports the network protocol (EtherType) and IP payload.
// Per-interface Linux captures are Ethernet (or raw IP for tun/wireguard/ppp), but the "any" pseudo device and some
// interfaces deliver Linux cooked captures (SLL, and SLL2 with libpcap >= 1.10), which the Windows-only code never saw.
using System;
using System.Buffers.Binary;
using Libpcap;

namespace StatisticsAnalysisTool.Linux.Capture;

internal static class LinkLayerDecoder
{
    private const ushort EtherTypeIPv4 = 0x0800;
    private const ushort EtherTypeIPv6 = 0x86DD;
    private const ushort EtherTypeVlan = 0x8100;
    private const ushort EtherTypeQinQ = 0x88A8;
    private const int EthernetHeaderLength = 14;
    private const int VlanTagLength = 4;
    private const int SllHeaderLength = 16;
    private const int Sll2HeaderLength = 20;

    public static bool TryRead(PcapDataLink dataLink, ReadOnlySpan<byte> data, out ushort etherType, out ReadOnlySpan<byte> payload)
    {
        etherType = 0;
        payload = default;

        return dataLink switch
        {
            PcapDataLink.DLT_EN10MB => TryReadEthernet(data, out etherType, out payload),
            PcapDataLink.DLT_LINUX_SLL => TryReadCooked(data, SllHeaderLength, 14, out etherType, out payload),
            PcapDataLink.DLT_LINUX_SLL2 => TryReadCooked(data, Sll2HeaderLength, 0, out etherType, out payload),
            PcapDataLink.DLT_RAW => TryReadRawIp(data, out etherType, out payload),
            _ => false
        };
    }

    private static bool TryReadEthernet(ReadOnlySpan<byte> data, out ushort etherType, out ReadOnlySpan<byte> payload)
    {
        etherType = 0;
        payload = default;

        if (data.Length < EthernetHeaderLength)
        {
            return false;
        }

        var offset = 12;
        etherType = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        while (etherType is EtherTypeVlan or EtherTypeQinQ)
        {
            if (data.Length < offset + VlanTagLength)
            {
                return false;
            }

            etherType = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 2, 2));
            offset += VlanTagLength;
        }

        payload = data[offset..];
        return true;
    }

    private static bool TryReadCooked(ReadOnlySpan<byte> data, int headerLength, int protocolOffset, out ushort etherType, out ReadOnlySpan<byte> payload)
    {
        etherType = 0;
        payload = default;

        if (data.Length < headerLength)
        {
            return false;
        }

        etherType = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(protocolOffset, 2));
        payload = data[headerLength..];
        return true;
    }

    private static bool TryReadRawIp(ReadOnlySpan<byte> data, out ushort etherType, out ReadOnlySpan<byte> payload)
    {
        etherType = 0;
        payload = default;

        if (data.IsEmpty)
        {
            return false;
        }

        etherType = (data[0] >> 4) switch
        {
            4 => EtherTypeIPv4,
            6 => EtherTypeIPv6,
            _ => (ushort) 0
        };

        if (etherType == 0)
        {
            return false;
        }

        payload = data;
        return true;
    }
}
