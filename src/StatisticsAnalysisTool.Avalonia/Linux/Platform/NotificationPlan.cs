// Pure decisions for notifications: which kinds are enabled, how they map to Avalonia, where they are shown.
using Avalonia.Controls.Notifications;
using System;
using System.Collections.Generic;

namespace StatisticsAnalysisTool.Linux.Platform;

public enum NotificationKind { Success, Error, TrackingStatus, Trade }

[Flags]
public enum NotificationChannels { None = 0, InApp = 1, Desktop = 2 }

internal static class NotificationPlan
{
    /// <summary>Same expiration as Notification.Wpf's default.</summary>
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(5);

    /// <summary>Success/Error/TrackingStatus follow the tracking-status switch; Trade follows the trade filter.</summary>
    public static bool IsEnabled(NotificationKind kind, bool trackingStatusActive, bool tradeActive)
        => kind == NotificationKind.Trade ? tradeActive : trackingStatusActive;

    public static NotificationType ToAvalonia(NotificationKind kind) => kind switch
    {
        NotificationKind.Error => NotificationType.Error,
        NotificationKind.TrackingStatus => NotificationType.Information,
        _ => NotificationType.Success
    };

    /// <summary>In-app toast while the window is visible; desktop notification when the user cannot see it (hidden or unfocused).</summary>
    public static NotificationChannels Route(bool windowVisible, bool windowActive, bool notifySendAvailable)
    {
        var channels = windowVisible ? NotificationChannels.InApp : NotificationChannels.None;
        if (notifySendAvailable && !(windowVisible && windowActive))
        {
            channels |= NotificationChannels.Desktop;
        }

        return channels;
    }

    public static IReadOnlyList<string> NotifySendArguments(NotificationKind kind, string title, string message) =>
    [
        "--app-name=Statistics Analysis Tool",
        kind == NotificationKind.Error ? "--urgency=critical" : "--urgency=normal",
        $"--expire-time={(int) Duration.TotalMilliseconds}",
        "--",
        title ?? string.Empty,
        EscapeMarkup(message ?? string.Empty)
    ];

    // Notification servers may interpret the body as markup (<b>, <a href>); show it literally.
    public static string EscapeMarkup(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
