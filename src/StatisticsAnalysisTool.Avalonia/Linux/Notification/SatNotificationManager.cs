// Linux replacement for Notification/SatNotificationManager.cs. The WPF version renders toasts through
// Notification.Wpf. Same public surface; messages are only logged until a Linux notifier exists.
// TODO(T17): show desktop notifications (freedesktop notifications / in-app toasts).
using Serilog;
using StatisticsAnalysisTool.Common.UserSettings;
using System.Threading.Tasks;

namespace StatisticsAnalysisTool.Notification;

public class SatNotificationManager
{
    private volatile bool _isShuttingDown;

    public Task ShowSuccessAsync(string title, string message) => ShowAsync("Success", title, message);

    public Task ShowErrorAsync(string title, string message) => ShowAsync("Error", title, message);

    public Task ShowTrackingStatusAsync(string title, string message) => ShowAsync("Information", title, message);

    public Task ShowTradeAsync(Trade.Trade trade)
    {
        if (!SettingsController.CurrentSettings.IsNotificationFilterTradeActive || trade == null || _isShuttingDown)
        {
            return Task.CompletedTask;
        }

        return ShowAsync("Success", trade.TradeNotificationTitleText, $"{trade.LocationName} - {trade.DisplayItemName}");
    }

    public void StopShowingNotifications()
    {
        _isShuttingDown = true;
    }

    private Task ShowAsync(string kind, string title, string message)
    {
        if (!_isShuttingDown)
        {
            Log.Information("Notification [{Kind}] {Title}: {Message}", kind, title, message);
        }

        return Task.CompletedTask;
    }
}
