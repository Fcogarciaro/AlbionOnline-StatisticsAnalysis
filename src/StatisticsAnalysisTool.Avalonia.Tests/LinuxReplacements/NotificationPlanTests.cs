using Avalonia.Controls.Notifications;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class NotificationPlanTests
{
    [TestCase(NotificationKind.Success, true, false, true)]
    [TestCase(NotificationKind.Error, false, true, false)]
    [TestCase(NotificationKind.TrackingStatus, true, false, true)]
    [TestCase(NotificationKind.Trade, true, false, false)]
    [TestCase(NotificationKind.Trade, false, true, true)]
    public void IsEnabled_MirrorsTheOriginalSettingGates(NotificationKind kind, bool tracking, bool trade, bool expected)
    {
        NotificationPlan.IsEnabled(kind, tracking, trade).Should().Be(expected);
    }

    [TestCase(NotificationKind.Success, NotificationType.Success)]
    [TestCase(NotificationKind.Error, NotificationType.Error)]
    [TestCase(NotificationKind.TrackingStatus, NotificationType.Information)]
    [TestCase(NotificationKind.Trade, NotificationType.Success)]
    public void ToAvalonia_MapsOriginalNotificationTypes(NotificationKind kind, NotificationType expected)
    {
        NotificationPlan.ToAvalonia(kind).Should().Be(expected);
    }

    [TestCase(true, true, true, NotificationChannels.InApp)]
    [TestCase(true, false, true, NotificationChannels.InApp | NotificationChannels.Desktop)]
    [TestCase(false, false, true, NotificationChannels.Desktop)]
    [TestCase(false, false, false, NotificationChannels.None)]
    [TestCase(true, false, false, NotificationChannels.InApp)]
    public void Route_UsesDesktopWhenTheUserCannotSeeTheWindow(bool visible, bool active, bool notifySend, NotificationChannels expected)
    {
        NotificationPlan.Route(visible, active, notifySend).Should().Be(expected);
    }

    [Test]
    public void NotifySendArguments_UrgencyFollowsKind_AndDashesInTextAreSafe()
    {
        var error = NotificationPlan.NotifySendArguments(NotificationKind.Error, "-Oops", "disk full");
        error.Should().Equal("--app-name=Statistics Analysis Tool", "--urgency=critical", "--expire-time=5000", "--", "-Oops", "disk full");

        NotificationPlan.NotifySendArguments(NotificationKind.Success, "t", "m")
            .Should().Contain("--urgency=normal");
    }

    [Test]
    public void NotifySendArguments_EscapesMarkupInTheBody_ButNotTheTitle()
    {
        var args = NotificationPlan.NotifySendArguments(NotificationKind.Success, "A & B", "<b>x</b> & y");
        args[^2].Should().Be("A & B");
        args[^1].Should().Be("&lt;b&gt;x&lt;/b&gt; &amp; y");
    }
}
