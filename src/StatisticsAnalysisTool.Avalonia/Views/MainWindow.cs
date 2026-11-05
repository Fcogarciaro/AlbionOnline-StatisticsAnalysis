// PLACEHOLDER for Views/MainWindow.xaml (T09 replaces this file with the real view). It already does what the WPF
// code-behind does outside the visual tree: restores the window geometry, owns the system tray service and the Albion
// process monitor, hosts the MainWindowViewModel as DataContext, hides to the tray on start and persists the window
// settings on close. Not ported yet: window chrome buttons, the uptime label and the stats drop-down handlers.
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Serilog;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Network.Manager;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Windows;

namespace StatisticsAnalysisTool.Views;

public class MainWindow : System.Windows.Window
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly SystemTrayService _systemTrayService;
    private readonly AlbionGameProcessMonitor _albionGameProcessMonitor;

    public MainWindow(MainWindowViewModel mainWindowViewModel)
    {
        Title = "Statistics Analysis Tool";
        Content = new TextBlock
        {
            Text = "Statistics Analysis Tool - main window (T09 ports the real view)",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.White
        };

        InitWindow();
        _systemTrayService = new SystemTrayService(this, mainWindowViewModel);
        _albionGameProcessMonitor = new AlbionGameProcessMonitor();
        ServiceLocator.Register<AlbionGameProcessMonitor>(_albionGameProcessMonitor);
        _albionGameProcessMonitor.GameStarted += AlbionGameProcessMonitor_OnGameStarted;
        _albionGameProcessMonitor.GameStopped += AlbionGameProcessMonitor_OnGameStopped;
        _mainWindowViewModel = mainWindowViewModel;
        DataContext = _mainWindowViewModel;
        Opened += MainWindow_OnOpened;
        Closing += MainWindow_OnClosing;
    }

    public void InitWindow()
    {
        var settings = SettingsController.CurrentSettings;
        Height = settings.MainWindowHeight;
        Width = settings.MainWindowWidth;
        Left = settings.MainWindowLeftPosition;
        Top = settings.MainWindowTopPosition;
        WindowStartupLocation = WindowStartupLocation.Manual;
        if (settings.MainWindowMaximized)
        {
            WindowState = global::Avalonia.Controls.WindowState.Maximized;
        }

        if (settings.MainWindowLeftPosition == 0 && settings.MainWindowTopPosition == 0)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private void MainWindow_OnClosing(object sender, WindowClosingEventArgs eventArgs)
    {
        var windowStateForPersistence = _systemTrayService.WindowStateForPersistence;
        Opened -= MainWindow_OnOpened;
        _albionGameProcessMonitor.GameStarted -= AlbionGameProcessMonitor_OnGameStarted;
        _albionGameProcessMonitor.GameStopped -= AlbionGameProcessMonitor_OnGameStopped;
        _albionGameProcessMonitor.Dispose();
        _systemTrayService.Dispose();
        _mainWindowViewModel.DisposeItemDetails();
        _mainWindowViewModel.CraftingBindings.DisposeLossExplorer();
        SettingsController.SetWindowSettings(windowStateForPersistence, Height, Width, Left, Top);
    }

    // WPF Loaded: runs once when the window is first shown.
    private void MainWindow_OnOpened(object sender, EventArgs eventArgs)
    {
        Opened -= MainWindow_OnOpened;

        var settings = SettingsController.CurrentSettings;
        _albionGameProcessMonitor.SetMonitoringEnabled(AlbionGameProcessMonitor.IsMonitoringRequired(settings));
        var shouldRemainVisibleForRunningGame = settings.IsOpenWithGameActive
                                                && _albionGameProcessMonitor.IsGameRunning;
        if (settings.IsStartInSystemTrayActive && !shouldRemainVisibleForRunningGame)
        {
            _systemTrayService.HideWindowInSystemTray(false);
        }
    }

    private async void AlbionGameProcessMonitor_OnGameStarted(object sender, EventArgs eventArgs)
    {
        var settings = SettingsController.CurrentSettings;
        if (settings.IsOpenWithGameActive)
        {
            _systemTrayService.RestoreWindowFromSystemTray();
        }

        if (!settings.IsStartTrackingWithGameActive
            || _mainWindowViewModel.IsTrackingActive
            || !ServiceLocator.IsServiceInDictionary<TrackingController>())
        {
            return;
        }

        try
        {
            await ServiceLocator.Resolve<TrackingController>().StartTrackingAsync();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Tracking could not be started when Albion Online started");
        }
    }

    private void AlbionGameProcessMonitor_OnGameStopped(object sender, EventArgs eventArgs)
    {
        var settings = SettingsController.CurrentSettings;
        if (settings.IsStopTrackingWithGameActive
            && _mainWindowViewModel.IsTrackingActive
            && ServiceLocator.IsServiceInDictionary<TrackingController>())
        {
            try
            {
                ServiceLocator.Resolve<TrackingController>().StopTracking();
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Tracking could not be stopped when Albion Online closed");
            }
        }

        if (settings.IsHideWithGameActive)
        {
            _systemTrayService.HideWindowInSystemTray(false);
        }
    }
}
