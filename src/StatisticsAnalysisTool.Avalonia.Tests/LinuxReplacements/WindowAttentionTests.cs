using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class WindowAttentionTests
{
    [Test]
    public void X11Handle_BuildsWmctrlDemandAttentionAddAndRemove()
    {
        WindowAttention.BuildWmctrlArguments("XID", 0x3a00007, true, true)
            .Should().Equal("-i", "-r", "0x3a00007", "-b", "add,demand_attention");
        WindowAttention.BuildWmctrlArguments("XID", 0x3a00007, false, true)
            .Should().Equal("-i", "-r", "0x3a00007", "-b", "remove,demand_attention");
    }

    [TestCase("XID", 0, true)]
    [TestCase("XID", 5, false)]
    [TestCase("HWND", 5, true)]
    [TestCase(null, 5, true)]
    public void Unsupported_ReturnsNull(string descriptor, int handle, bool wmctrl)
    {
        WindowAttention.BuildWmctrlArguments(descriptor, handle, true, wmctrl).Should().BeNull();
    }
}
