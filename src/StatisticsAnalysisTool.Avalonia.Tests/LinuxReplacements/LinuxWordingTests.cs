using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Avalonia.Tests.Views;
using StatisticsAnalysisTool.Linux.Localization;
using StatisticsAnalysisTool.Localization;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

[NonParallelizable]
public class LinuxWordingTests
{
    private static readonly string[] WindowsOnlyWords = ["Windows", "notification area", "Npcap", "Administrator", "taskbar"];

    [TestCase("START_WITH_WINDOWS")]
    [TestCase("START_WITH_WINDOWS_DESCRIPTION")]
    [TestCase("START_IN_SYSTEM_TRAY")]
    [TestCase("START_IN_SYSTEM_TRAY_DESCRIPTION")]
    [TestCase("OPEN_WITH_GAME_DESCRIPTION")]
    [TestCase("HIDE_WITH_GAME_DESCRIPTION")]
    [TestCase("MINIMIZE_TO_SYSTEM_TRAY")]
    [TestCase("APPLICATION_CONTINUES_TO_RUN_IN_SYSTEM_TRAY")]
    [TestCase("WINDOWS_STARTUP_SETTING_UPDATE_FAILED")]
    [TestCase("CREATE_DESKTOP_SHORTCUT")]
    [TestCase("TOOL_MUST_BE_RUN_AS_ADMIN")]
    [TestCase("ERR_SOCKET_FAILED_WITH_CODE")]
    [TestCase("PACKET_HANDLER_ERROR_MESSAGE")]
    public void WindowsKeys_AreOverriddenWithLinuxWording(string key)
    {
        LinuxWording.TryGetTextOverride(key, out var text).Should().BeTrue();

        text.Should().NotBeNullOrWhiteSpace();
        foreach (var word in WindowsOnlyWords)
        {
            text.Should().NotContainEquivalentOf(word, $"{key} must not talk about Windows");
        }
    }

    [Test]
    public void TheSocketErrorKeepsItsFormatPlaceholder()
    {
        LinuxWording.TryGetTextOverride("ERR_SOCKET_FAILED_WITH_CODE", out var text).Should().BeTrue();

        string.Format(text, "AccessDenied").Should().Contain("AccessDenied");
    }

    [Test]
    public void OtherKeysAreLeftAlone()
    {
        LinuxWording.TryGetTextOverride("LOSS_EXPLORER", out _).Should().BeFalse();
        LinuxWording.TryGetTextOverride(null, out _).Should().BeFalse();
    }

    [Test]
    public void LocalizationController_ServesTheOverrides()
    {
        using var harness = new ViewHarness();

        LocalizationController.Translation("START_WITH_WINDOWS").Should().Be("Start at login");
        LocalizationController.Translation("START_IN_SYSTEM_TRAY").Should().Contain("system tray");
    }
}
