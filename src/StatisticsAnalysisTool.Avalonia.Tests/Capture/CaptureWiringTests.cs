using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Core;
using StatisticsAnalysisTool.Linux.Capture;
using StatisticsAnalysisTool.Localization;

namespace StatisticsAnalysisTool.Avalonia.Tests.Capture;

public class CaptureWiringTests
{
    [Test]
    public void IsAppStartedAsAdministrator_MeansCaptureCapableOnLinux()
    {
        ApplicationCore.IsAppStartedAsAdministrator().Should().Be(CapturePrivilege.IsCurrentProcessCapable());
    }

    [Test]
    public void Translation_UsesTheLinuxGuidanceForTheNpcapKeys()
    {
        LocalizationController.Translation("ERR_NPCAP_DLL_MISSING").Should().Be(CaptureGuidance.LibraryMissingMessage);
        LocalizationController.Translation("NPCAP_INFO_DIALOG_LINK_TEXT").Should().Be("About Linux capabilities");
    }

    [Test]
    public void Translation_ShowsTheSetcapCommandForNoListeningAdaptersWhenNotCapable()
    {
        if (CapturePrivilege.IsCurrentProcessCapable())
        {
            Assert.Ignore("the test process is capable; the permission text is not shown");
        }

        LocalizationController.Translation("NO_LISTENING_ADAPTERS").Should().Contain("sudo setcap cap_net_raw,cap_net_admin=eip");
    }
}
