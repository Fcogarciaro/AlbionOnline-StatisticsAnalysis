using FluentAssertions;
using Libpcap;
using NUnit.Framework;
using StatisticsAnalysisTool.Network.PacketProviders;

namespace StatisticsAnalysisTool.Avalonia.Tests.Capture;

public class LibpcapProviderLinkTypeTests
{
    [Test]
    public void TryReadNetworkLayer_DecodesLinuxCookedSll2Frames()
    {
        var ip = new byte[] { 0x45, 0x00, 0x00, 0x1c, 0, 0, 0, 0, 0x40, 0x11, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        var frame = new byte[] { 0x08, 0x00, 0, 0, 0, 0, 0, 2, 0, 1, 0, 6, 2, 0, 0, 0, 0, 1, 0, 0 }.Concat(ip).ToArray();

        var ok = LibpcapPacketProvider.TryReadNetworkLayer(PcapDataLink.DLT_LINUX_SLL2, frame, out var etherType, out var payload);

        ok.Should().BeTrue();
        etherType.Should().Be(0x0800);
        payload.ToArray().Should().Equal(ip);
    }

    [Test]
    public void TryReadNetworkLayer_DecodesLinuxCookedSllFrames()
    {
        var ip = new byte[] { 0x60, 0, 0, 0, 0, 8, 0x11, 0x40 };
        var frame = new byte[] { 0, 4, 0, 1, 0, 6, 2, 0, 0, 0, 0, 1, 0, 0, 0x86, 0xDD }.Concat(ip).ToArray();

        var ok = LibpcapPacketProvider.TryReadNetworkLayer(PcapDataLink.DLT_LINUX_SLL, frame, out var etherType, out var payload);

        ok.Should().BeTrue();
        etherType.Should().Be(0x86DD);
        payload.ToArray().Should().Equal(ip);
    }
}
