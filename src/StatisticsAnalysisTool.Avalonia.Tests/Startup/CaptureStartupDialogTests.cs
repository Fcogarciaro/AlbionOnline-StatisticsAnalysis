using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Capture;
using StatisticsAnalysisTool.Linux.Startup;

namespace StatisticsAnalysisTool.Avalonia.Tests.Startup;

public class CaptureStartupDialogTests
{
    [Test]
    public void Capable_ShowsTheMessageAndTheCapabilitiesLink_NoNpcap()
    {
        var (title, text) = CaptureStartupDialog.Build("T", "ready", "About", new CaptureEnvironment(true, "/opt/sat/sat", string.Empty));

        title.Should().Be("T");
        text.Should().StartWith("ready").And.EndWith("About: https://man7.org/linux/man-pages/man7/capabilities.7.html");
        text.ToLowerInvariant().Should().NotContain("npcap");
    }

    [Test]
    public void BuildContent_KeepsTheLinkSeparateForTheClickableDialogLink()
    {
        var content = CaptureStartupDialog.BuildContent("T", "ready", "About", new CaptureEnvironment(true, "/opt/sat/sat", string.Empty));

        content.Title.Should().Be("T");
        content.Message.Should().Be("ready");
        content.LinkText.Should().Be("About");
        content.Url.Should().Be("https://man7.org/linux/man-pages/man7/capabilities.7.html");
    }

    [Test]
    public void BuildContent_NotCapable_PutsTheSetcapCommandIntoTheMessage()
    {
        var content = CaptureStartupDialog.BuildContent("T", "devices listed", "About", new CaptureEnvironment(false, "/opt/sat/sat", string.Empty));

        content.Message.Should().StartWith("devices listed").And.Contain("sudo setcap cap_net_raw,cap_net_admin=eip /opt/sat/sat");
        content.Message.Should().NotContain("man7.org");
    }

    [Test]
    public void NotCapable_AppendsTheSetcapMessage_EvenWhenTheMessageDoesNotHaveIt()
    {
        var (_, text) = CaptureStartupDialog.Build("T", "devices listed", "About", new CaptureEnvironment(false, "/opt/sat/sat", string.Empty));

        text.Should().Contain("devices listed").And.Contain("sudo setcap cap_net_raw,cap_net_admin=eip /opt/sat/sat");
    }

    [Test]
    public void NotCapable_DoesNotRepeatAMessageThatAlreadyCarriesTheGuidance()
    {
        var environment = new CaptureEnvironment(false, "/opt/sat/sat", string.Empty);
        var permission = CaptureGuidance.PermissionMessage(environment.ProcessPath, environment.EntryAssemblyPath);

        var (_, text) = CaptureStartupDialog.Build("T", permission, "About", environment);

        text.Split("sudo setcap").Length.Should().Be(2);
    }
}
