using FluentAssertions;
using Libpcap;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Capture;

namespace StatisticsAnalysisTool.Avalonia.Tests.Capture;

// Byte fixtures follow the frame layouts libpcap hands out for each link type
// (https://www.tcpdump.org/linktypes/LINKTYPE_LINUX_SLL.html and ..._SLL2.html).
public class LinkLayerDecoderTests
{
    private static readonly byte[] Ipv4Packet = [0x45, 0x00, 0x00, 0x1c, 0xde, 0xad, 0x00, 0x00, 0x40, 0x11, 0x00, 0x00, 10, 0, 0, 1, 10, 0, 0, 2];
    private static readonly byte[] Ipv6Packet = [0x60, 0x00, 0x00, 0x00, 0x00, 0x08, 0x11, 0x40];

    private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    private static readonly byte[] EthernetAddresses = [0x02, 0, 0, 0, 0, 0x01, 0x02, 0, 0, 0, 0, 0x02];

    private static (bool Ok, ushort EtherType, byte[] Payload) Decode(PcapDataLink link, byte[] frame)
    {
        var ok = LinkLayerDecoder.TryRead(link, frame, out var etherType, out var payload);
        return (ok, etherType, payload.ToArray());
    }

    [Test]
    public void Ethernet_ReturnsTheEtherTypeAndPayload()
    {
        var frame = Concat(EthernetAddresses, [0x08, 0x00], Ipv4Packet);

        var (ok, etherType, payload) = Decode(PcapDataLink.DLT_EN10MB, frame);

        ok.Should().BeTrue();
        etherType.Should().Be(0x0800);
        payload.Should().Equal(Ipv4Packet);
    }

    [Test]
    public void Ethernet_SkipsVlanTags()
    {
        var frame = Concat(EthernetAddresses, [0x81, 0x00, 0x00, 0x64, 0x86, 0xDD], Ipv6Packet);

        var (ok, etherType, payload) = Decode(PcapDataLink.DLT_EN10MB, frame);

        ok.Should().BeTrue();
        etherType.Should().Be(0x86DD);
        payload.Should().Equal(Ipv6Packet);
    }

    [Test]
    public void LinuxCooked_Sll_ReadsTheProtocolAtOffset14()
    {
        // packet type 4 (outgoing), ARPHRD_ETHER, address length 6, address (8 bytes), protocol 0x0800
        var header = new byte[] { 0x00, 0x04, 0x00, 0x01, 0x00, 0x06, 0x02, 0, 0, 0, 0, 0x01, 0, 0, 0x08, 0x00 };
        var frame = Concat(header, Ipv4Packet);

        var (ok, etherType, payload) = Decode(PcapDataLink.DLT_LINUX_SLL, frame);

        ok.Should().BeTrue();
        etherType.Should().Be(0x0800);
        payload.Should().Equal(Ipv4Packet);
    }

    [Test]
    public void LinuxCooked_Sll2_ReadsTheProtocolAtOffset0AndSkips20Bytes()
    {
        // protocol 0x86DD, reserved, interface index 2, ARPHRD_ETHER, packet type 0, address length 6, address (8 bytes)
        var header = new byte[] { 0x86, 0xDD, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, 0x00, 0x06, 0x02, 0, 0, 0, 0, 0x01, 0, 0 };
        var frame = Concat(header, Ipv6Packet);

        var (ok, etherType, payload) = Decode(PcapDataLink.DLT_LINUX_SLL2, frame);

        ok.Should().BeTrue();
        etherType.Should().Be(0x86DD);
        payload.Should().Equal(Ipv6Packet);
    }

    [Test]
    public void Raw_DetectsTheIpVersionFromTheFirstNibble()
    {
        var v4 = Decode(PcapDataLink.DLT_RAW, Ipv4Packet);
        var v6 = Decode(PcapDataLink.DLT_RAW, Ipv6Packet);

        (v4.Ok, v4.EtherType).Should().Be((true, (ushort) 0x0800));
        v4.Payload.Should().Equal(Ipv4Packet);
        (v6.Ok, v6.EtherType).Should().Be((true, (ushort) 0x86DD));
        v6.Payload.Should().Equal(Ipv6Packet);
    }

    [TestCase(PcapDataLink.DLT_EN10MB, 13)]
    [TestCase(PcapDataLink.DLT_LINUX_SLL, 15)]
    [TestCase(PcapDataLink.DLT_LINUX_SLL2, 19)]
    [TestCase(PcapDataLink.DLT_RAW, 0)]
    public void TruncatedFrames_AreRejected(PcapDataLink link, int length)
    {
        LinkLayerDecoder.TryRead(link, new byte[length], out _, out _).Should().BeFalse();
    }

    [Test]
    public void UnsupportedLinkTypes_AreRejected()
    {
        LinkLayerDecoder.TryRead(PcapDataLink.DLT_IEEE802_11, new byte[64], out _, out _).Should().BeFalse();
    }
}
