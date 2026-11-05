using FluentAssertions;
using Libpcap;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Capture;

namespace StatisticsAnalysisTool.Avalonia.Tests.Capture;

public class LibpcapLibraryTests
{
    [Test]
    public void TryLoad_IgnoresLibrariesOtherThanPcap()
    {
        var calls = new List<string>();

        var handle = LibpcapLibrary.TryLoad("sqlite3", name => { calls.Add(name); return 1; });

        handle.Should().Be(IntPtr.Zero);
        calls.Should().BeEmpty();
    }

    [Test]
    public void TryLoad_TriesTheVersionedSonameFirstAndFallsBack()
    {
        var calls = new List<string>();

        var handle = LibpcapLibrary.TryLoad("pcap", name =>
        {
            calls.Add(name);
            return name == "libpcap.so.0.8" ? 42 : IntPtr.Zero;
        });

        handle.Should().Be(42);
        calls.Should().Equal("libpcap.so.1", "libpcap.so", "libpcap.so.0.8");
    }

    [Test]
    public void TryLoad_ReturnsZeroWhenNothingLoads()
    {
        LibpcapLibrary.TryLoad("pcap", _ => IntPtr.Zero).Should().Be(IntPtr.Zero);
    }

    [Test]
    public void ListDevices_WorksWithoutPrivilegesOnceTheResolverIsInstalled()
    {
        if (!LibpcapLibrary.IsInstalledOnThisMachine())
        {
            Assert.Ignore("libpcap is not installed on this machine");
        }

        LibpcapLibrary.Install();
        LibpcapLibrary.Install(); // idempotent

        var devices = Pcap.ListDevices();

        devices.Should().NotBeNull();
    }
}
