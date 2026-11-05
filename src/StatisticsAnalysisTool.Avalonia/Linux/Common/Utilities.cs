// Linux replacement for Common/Utilities.cs. Identical to the WPF file except for the window helpers
// (user32 FlashWindowEx, WindowInteropHelper, SystemParameters), which are Avalonia/no-op equivalents.
// Keep in sync with src/StatisticsAnalysisTool/Common/Utilities.cs when upstream changes.
using Serilog;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Notification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using StatisticsAnalysisTool.Diagnostics;

namespace StatisticsAnalysisTool.Common;

public static class Utilities
{
    public static long GetHighestLength(params Array[] arrays)
    {
        long highestLength = 0;

        foreach (var array in arrays)
        {
            if (array.Length > highestLength)
            {
                highestLength = array.Length;
            }
        }

        return highestLength;
    }

    public static void CenterWindowOnScreen(Window window)
    {
        var screen = window.Screens?.Primary;
        if (screen == null)
        {
            return;
        }

        var scale = screen.Scaling;
        var area = screen.WorkingArea;
        window.Position = new global::Avalonia.PixelPoint(
            area.X + (int) ((area.Width - (window.Width * scale)) / 2),
            area.Y + (int) ((area.Height - (window.Height * scale)) / 2));
    }

    public static bool IsWindowOpen<T>(string name = "") where T : Window
    {
        return string.IsNullOrEmpty(name)
            ? Application.Current.Windows.OfType<T>().Any()
            : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
    }

    public static double GetValuePerSecondToDouble(double value, DateTime? combatStart, TimeSpan time, double maxValue = -1)
    {
        if (double.IsInfinity(value)) return maxValue > 0 ? maxValue : double.MaxValue;

        if (time.Ticks <= 1 && combatStart != null)
        {
            var startTimeSpan = DateTime.UtcNow - (DateTime) combatStart;
            var calculation = value / startTimeSpan.TotalSeconds;
            return calculation > maxValue ? maxValue : calculation;
        }

        var valuePerSeconds = value / time.TotalSeconds;
        if (maxValue > 0 && valuePerSeconds > maxValue) return maxValue;

        return valuePerSeconds;
    }

    public static bool IsBlockingTimeExpired(DateTime dateTime, int waitingSeconds)
    {
        var currentDateTime = DateTime.UtcNow;
        var difference = currentDateTime.Subtract(dateTime);
        return difference.Seconds >= waitingSeconds;
    }

    public static void AnotherAppToStart(string path)
    {
        var notifyManager = ServiceLocator.Resolve<SatNotificationManager>();

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            if (!File.Exists(path))
            {
                notifyManager?.ShowErrorAsync(LocalizationController.Translation("CANNOT_START_OTHER_APP"),
                    LocalizationController.Translation("CAN_NOT_START_APP_WITH_PATH",
                        new List<string> { "path" },
                        new List<string> { path }));
                return;
            }

            Process.Start(path);
        }
        catch (Exception e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
            notifyManager?.ShowErrorAsync(LocalizationController.Translation("CANNOT_START_OTHER_APP"),
                LocalizationController.Translation("CAN_NOT_START_APP_WITH_PATH",
                    new List<string> { "path" },
                    new List<string> { path }));
        }
    }

    #region Window Flash

    // TODO(T18): request user attention through the window manager (e.g. _NET_WM_STATE_DEMANDS_ATTENTION).
    public static void FlashWindow(this Window win, uint count = uint.MaxValue)
    {
    }

    public static void StopFlashingWindow(this Window win)
    {
    }

    #endregion
}