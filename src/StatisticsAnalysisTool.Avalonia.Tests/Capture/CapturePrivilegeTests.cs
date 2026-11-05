using FluentAssertions;
using Libpcap;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Capture;

namespace StatisticsAnalysisTool.Avalonia.Tests.Capture;

public class CapturePrivilegeTests
{
    private static string Status(string capEff) =>
        $"Name:\tStatisticsAnalysisTool\nUmask:\t0022\nCapInh:\t0000000000000000\nCapPrm:\t{capEff}\nCapEff:\t{capEff}\nCapBnd:\t000001ffffffffff\n";

    [Test]
    public void ParseCapEff_ReadsTheHexMaskOfTheCapEffLine()
    {
        CapturePrivilege.ParseCapEff(Status("0000000000003000")).Should().Be(0x3000UL);
        CapturePrivilege.ParseCapEff(Status("000001ffffffffff")).Should().Be(0x1ffffffffffUL);
    }

    [TestCase("")]
    [TestCase("Name:\tx\nCapPrm:\t0000000000003000\n")]
    [TestCase("CapEff:\tnot-hex\n")]
    [TestCase(null)]
    public void ParseCapEff_ReturnsNullWhenTheLineIsMissingOrMalformed(string content)
    {
        CapturePrivilege.ParseCapEff(content).Should().BeNull();
    }

    [TestCase(0x3000UL, true)]   // CAP_NET_ADMIN (12) + CAP_NET_RAW (13)
    [TestCase(0x2000UL, true)]   // CAP_NET_RAW only: enough to capture, NET_ADMIN is optional
    [TestCase(0x1000UL, false)]  // CAP_NET_ADMIN only
    [TestCase(0UL, false)]
    [TestCase(0x1ffffffffffUL, true)]
    public void HasCaptureCapabilities_NeedsNetRawOnly(ulong capEff, bool expected)
    {
        CapturePrivilege.HasCaptureCapabilities(capEff).Should().Be(expected);
    }

    [Test]
    public void IsCapable_TrueForRootEvenWithoutAReadableStatusFile()
    {
        CapturePrivilege.IsCapable(isRoot: true, procStatus: null).Should().BeTrue();
    }

    [Test]
    public void IsCapable_ForNonRootFollowsCapEff()
    {
        CapturePrivilege.IsCapable(false, Status("0000000000003000")).Should().BeTrue();
        CapturePrivilege.IsCapable(false, Status("0000000000002000")).Should().BeTrue();
        CapturePrivilege.IsCapable(false, Status("0000000000001000")).Should().BeFalse();
        CapturePrivilege.IsCapable(false, Status("0000000000000000")).Should().BeFalse();
        CapturePrivilege.IsCapable(false, null).Should().BeFalse();
    }

    [Test]
    public void OpeningADevice_SucceedsWhenCapableAndFailsGracefullyOtherwise()
    {
        if (!LibpcapLibrary.IsInstalledOnThisMachine())
        {
            Assert.Ignore("libpcap is not installed on this machine");
        }

        LibpcapLibrary.Install();
        var device = Pcap.ListDevices().FirstOrDefault(d =>
            d.Flags.HasFlag(PcapDeviceFlags.Up) && !d.Flags.HasFlag(PcapDeviceFlags.Loopback) && d.Name != "any");
        if (device is null)
        {
            Assert.Ignore("no up network interface to open");
        }

        var open = () =>
        {
            using var pcap = Pcap.OpenDevice(device);
            pcap.Activate();
        };

        if (CapturePrivilege.IsCurrentProcessCapable())
        {
            open.Should().NotThrow();
        }
        else
        {
            // upstream maps this exception type name to the ERR_NPCAP_OPEN_FAILED message (see TrackingController)
            open.Should().Throw<Exception>("an unprivileged process must get a permission error, not a crash")
                .Where(e => e.GetType().Name == "PcapException" && e.Message.Contains("CAP_NET_RAW"));
        }
    }
}
