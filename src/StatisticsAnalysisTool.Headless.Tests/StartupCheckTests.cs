using System.Text.Json;
using StatisticsAnalysisTool.Headless.Contract;

namespace StatisticsAnalysisTool.Headless.Tests;

public class StartupCheckTests
{
    [Fact]
    public void MissingPrivilege_EmitsNoPrivilegeWithSetcapCommandAndExitCode2()
    {
        var sink = new RecordingSink();

        var exit = StartupCheck.Run(isCapable: false, "/opt/sat/sat-headless", sink);

        Assert.Equal(2, exit);
        var status = sink.Single("status");
        Assert.Equal("no_privilege", status.GetProperty("state").GetString());
        Assert.Contains("sudo setcap cap_net_raw,cap_net_admin=eip /opt/sat/sat-headless", status.GetProperty("message").GetString());
    }

    [Fact]
    public void Privileged_EmitsNothingAndReturnsZero()
    {
        var sink = new RecordingSink();

        var exit = StartupCheck.Run(isCapable: true, "/opt/sat/sat-headless", sink);

        Assert.Equal(0, exit);
        Assert.Empty(sink.Lines);
    }

    [Fact]
    public void SetcapCommand_QuotesPathsWithSpaces()
    {
        Assert.Equal("sudo setcap cap_net_raw,cap_net_admin=eip '/home/a b/sat-headless'", StartupCheck.SetcapCommand("/home/a b/sat-headless"));
    }
}
