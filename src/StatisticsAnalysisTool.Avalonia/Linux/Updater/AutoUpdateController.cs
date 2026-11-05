// Linux replacement for Updater/AutoUpdateController.cs (and UpdateDownloadSession.cs).
// The WPF implementation is built on NetSparkleUpdater.UI.WPF, Authenticode verification and the WPF update
// window, none of which exist on Linux. Same public surface as the original so callers (FooterViewModel,
// SettingsControl, App startup) compile unchanged.
// TODO(T03/T17): query GitHub releases and surface the result; until then no update is ever reported.
using System;
using System.Threading.Tasks;
using Serilog;

namespace StatisticsAnalysisTool.Updater;

public static class AutoUpdateController
{
    private static bool _isUpdateAvailable;

    public static event Action<bool> UpdateAvailabilityChanged;

    public static bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        private set
        {
            if (_isUpdateAvailable == value)
            {
                return;
            }

            _isUpdateAvailable = value;
            UpdateAvailabilityChanged?.Invoke(value);
        }
    }

    public static Task StartBackgroundUpdateLoopAsync()
    {
        Log.Information("Automatic update checks are not available on Linux yet; use the package manager or GitHub releases.");
        return Task.CompletedTask;
    }

    public static Task CheckForUpdatesAsync()
    {
        IsUpdateAvailable = false;
        return Task.CompletedTask;
    }

    public static Task ShowAvailableUpdateWindowAsync() => Task.CompletedTask;

    public static void Dispose()
    {
    }
}
