using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Capture;

namespace StatisticsAnalysisTool.Avalonia.Tests.Capture;

public class CaptureGuidanceTests
{
    [Test]
    public void SetcapTarget_IsTheProcessPathForTheApphost()
    {
        CaptureGuidance.SetcapTarget("/opt/sat/StatisticsAnalysisTool", "/opt/sat/StatisticsAnalysisTool.dll")
            .Should().Be("/opt/sat/StatisticsAnalysisTool");
    }

    [Test]
    public void SetcapTarget_PointsAtTheApphostNextToTheDllWhenStartedThroughDotnet()
    {
        CaptureGuidance.SetcapTarget("/usr/bin/dotnet", "/opt/sat/StatisticsAnalysisTool.dll")
            .Should().Be("/opt/sat/StatisticsAnalysisTool");
    }

    [Test]
    public void SetcapCommand_QuotesPathsWithSpaces()
    {
        CaptureGuidance.SetcapCommand("/opt/my apps/StatisticsAnalysisTool")
            .Should().Be("sudo setcap cap_net_raw,cap_net_admin=eip '/opt/my apps/StatisticsAnalysisTool'");
        CaptureGuidance.SetcapCommand("/opt/sat/StatisticsAnalysisTool")
            .Should().Be("sudo setcap cap_net_raw,cap_net_admin=eip /opt/sat/StatisticsAnalysisTool");
        CaptureGuidance.SetcapCommand("/opt/o'brien/sat")
            .Should().Be("sudo setcap cap_net_raw,cap_net_admin=eip '/opt/o'\\''brien/sat'");
    }

    [Test]
    public void PermissionMessage_ContainsTheExactCommandAndExplainsTheDotnetHost()
    {
        var message = CaptureGuidance.PermissionMessage("/usr/bin/dotnet", "/opt/sat/StatisticsAnalysisTool.dll");

        message.Should().Contain("sudo setcap cap_net_raw,cap_net_admin=eip /opt/sat/StatisticsAnalysisTool");
        message.Should().Contain("dotnet").And.Contain("StatisticsAnalysisTool");
    }

    [Test]
    public void PermissionMessage_OmitsTheDotnetNoteForTheApphost()
    {
        var message = CaptureGuidance.PermissionMessage("/opt/sat/StatisticsAnalysisTool", "/opt/sat/StatisticsAnalysisTool.dll");

        message.Should().Contain("sudo setcap cap_net_raw,cap_net_admin=eip /opt/sat/StatisticsAnalysisTool");
        message.Should().NotContain("dotnet");
    }

    [Test]
    public void LibraryMissingMessage_NamesThePackagesOfCommonDistributions()
    {
        CaptureGuidance.LibraryMissingMessage.Should().Contain("libpcap").And.Contain("pacman").And.Contain("apt");
    }

    [TestCase("NO_LISTENING_ADAPTERS")]
    [TestCase("ERR_NPCAP_OPEN_FAILED")]
    [TestCase("START_APPLICATION_AS_ADMINISTRATOR")]
    public void Overrides_ShowThePermissionMessageOnlyWhenNotCapable(string key)
    {
        var missing = new CaptureEnvironment(false, "/opt/sat/StatisticsAnalysisTool", "/opt/sat/StatisticsAnalysisTool.dll");
        var capable = missing with { IsCapable = true };

        CaptureGuidance.TryGetTextOverride(key, missing, out var text).Should().BeTrue();
        text.Should().Contain("setcap");
        CaptureGuidance.TryGetTextOverride(key, capable, out _).Should().BeFalse();
    }

    [TestCase("NPCAP_INFO_DIALOG_TITLE")]
    [TestCase("NPCAP_INFO_DIALOG_MESSAGE")]
    [TestCase("NPCAP_INFO_DIALOG_LINK_TEXT")]
    [TestCase("ERR_NPCAP_DLL_MISSING")]
    [TestCase("MAKE_SURE_YOU_HAVE_INSTALLED_NPCAP")]
    [TestCase("DESCRIPTION_TRACK_MY_GAME_DETAILS_1")]
    [TestCase("DESCRIPTION_TRACK_MY_GAME_DETAILS_2")]
    public void Overrides_ReplaceTheNpcapTextsEvenWhenCapable(string key)
    {
        var capable = new CaptureEnvironment(true, "/opt/sat/StatisticsAnalysisTool", "/opt/sat/StatisticsAnalysisTool.dll");

        CaptureGuidance.TryGetTextOverride(key, capable, out var text).Should().BeTrue();
        text.Should().NotContainEquivalentOf("npcap");
    }

    [Test]
    public void Overrides_IgnoreOtherKeys()
    {
        CaptureGuidance.TryGetTextOverride("SOME_OTHER_KEY", new CaptureEnvironment(false, "/x", "/x.dll"), out _).Should().BeFalse();
    }
}
