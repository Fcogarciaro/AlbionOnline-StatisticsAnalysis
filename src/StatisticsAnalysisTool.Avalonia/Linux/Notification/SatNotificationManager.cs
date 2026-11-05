// Linux replacement for Notification/SatNotificationManager.cs. The WPF version renders toasts through
// Notification.Wpf. Same public surface and setting gates; toasts are Avalonia WindowNotificationManager cards on
// the main window, plus a desktop notification via notify-send when the window is hidden or unfocused.
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Serilog;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Linux.Platform;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AvaloniaApplication = Avalonia.Application;

namespace StatisticsAnalysisTool.Notification;

public class SatNotificationManager
{
    private volatile bool _isShuttingDown;
    private WindowNotificationManager _inApp;
    private Window _inAppHost;

    public Task ShowSuccessAsync(string title, string message) => ShowAsync(NotificationKind.Success, title, message);

    public Task ShowErrorAsync(string title, string message) => ShowAsync(NotificationKind.Error, title, message);

    public Task ShowTrackingStatusAsync(string title, string message) => ShowAsync(NotificationKind.TrackingStatus, title, message);

    public Task ShowTradeAsync(Trade.Trade trade)
    {
        return trade == null
            ? Task.CompletedTask
            : ShowAsync(NotificationKind.Trade, trade.TradeNotificationTitleText, $"{trade.LocationName} - {trade.DisplayItemName}");
    }

    public void StopShowingNotifications()
    {
        _isShuttingDown = true;
    }

    private async Task ShowAsync(NotificationKind kind, string title, string message)
    {
        var settings = SettingsController.CurrentSettings;
        if (_isShuttingDown
            || !NotificationPlan.IsEnabled(kind, settings.IsNotificationTrackingStatusActive, settings.IsNotificationFilterTradeActive))
        {
            return;
        }

        Log.Information("Notification [{Kind}] {Title}: {Message}", kind, title, message);

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowOnUiThread(kind, title, message));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ShowOnUiThread(NotificationKind kind, string title, string message)
    {
        var host = (AvaloniaApplication.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var visible = host is { IsVisible: true };
        var channels = NotificationPlan.Route(visible, visible && host.IsActive, ExecutableLocator.IsAvailable("notify-send"));

        if (_isShuttingDown)
        {
            return;
        }

        if (channels.HasFlag(NotificationChannels.InApp))
        {
            GetInAppManager(host).Show(new global::Avalonia.Controls.Notifications.Notification(title, message, NotificationPlan.ToAvalonia(kind), NotificationPlan.Duration));
        }

        if (channels.HasFlag(NotificationChannels.Desktop))
        {
            SendDesktopNotification(kind, title, message);
        }
    }

    private WindowNotificationManager GetInAppManager(Window host)
    {
        if (_inApp == null || !ReferenceEquals(_inAppHost, host))
        {
            _inAppHost = host;
            _inApp = new WindowNotificationManager(host) { Position = NotificationPosition.BottomRight, MaxItems = 5 };
        }

        return _inApp;
    }

    private static void SendDesktopNotification(NotificationKind kind, string title, string message)
    {
        try
        {
            var startInfo = new ProcessStartInfo("notify-send") { UseShellExecute = false, CreateNoWindow = true };
            foreach (var argument in NotificationPlan.NotifySendArguments(kind, title, message))
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo);
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Log.Warning(e, "notify-send failed");
        }
    }
}
