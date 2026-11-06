using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Serilog;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Linux.Startup;
using StatisticsAnalysisTool.Network.Manager;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Windows;
using System.Windows.Threading;
using AvaloniaWindowState = Avalonia.Controls.WindowState;

namespace StatisticsAnalysisTool.Views;

/// <summary>
/// Port of Views/MainWindow.xaml(.cs). The window draws its own caption: ExtendClientAreaToDecorationsHint with NoChrome
/// (theme WindowChrome), drag by BeginMoveDrag on the header, resize through the handles of the XAML.
/// </summary>
public partial class MainWindow : System.Windows.Window
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly WindowChromeController _windowChromeController;
    private readonly DispatcherTimer _applicationUptimeTimer;
    private readonly SystemTrayService _systemTrayService;
    private readonly AlbionGameProcessMonitor _albionGameProcessMonitor;

    public MainWindow(MainWindowViewModel mainWindowViewModel)
    {
        InitializeComponent();
        _applicationUptimeTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _applicationUptimeTimer.Tick += ApplicationUptimeTimer_OnTick;
        _windowChromeController = new WindowChromeController(
            this,
            MaximizedButton,
            ResizeMode.CanResizeWithGrip,
            ResizeMode.NoResize);
        ResizeMode = ResizeMode.CanResizeWithGrip;
        InitWindow();
        _systemTrayService = new SystemTrayService(this, mainWindowViewModel);
        _albionGameProcessMonitor = new AlbionGameProcessMonitor();
        ServiceLocator.Register<AlbionGameProcessMonitor>(_albionGameProcessMonitor);
        _albionGameProcessMonitor.GameStarted += AlbionGameProcessMonitor_OnGameStarted;
        _albionGameProcessMonitor.GameStopped += AlbionGameProcessMonitor_OnGameStopped;
        _mainWindowViewModel = mainWindowViewModel;
        DataContext = _mainWindowViewModel;
        Opened += MainWindow_OnLoaded;
        Closing += MainWindow_OnClosing;
        UpdateApplicationUptime();
        _applicationUptimeTimer.Start();
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
            WindowState = AvaloniaWindowState.Maximized;
        }

        // Upstream tests MainWindowLeftPosition twice; both coordinates at 0 means "never positioned".
        if (settings.MainWindowLeftPosition == 0 && settings.MainWindowTopPosition == 0)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private void Hotbar_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        _windowChromeController.ToggleMaximizeOnDoubleClick(e);
        if (!e.Handled)
        {
            _windowChromeController.DragMoveOnMouseDown(e);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current?.Shutdown();
    }

    private void MainWindow_OnClosing(object sender, WindowClosingEventArgs eventArgs)
    {
        var windowStateForPersistence = _systemTrayService.WindowStateForPersistence;
        Opened -= MainWindow_OnLoaded;
        _applicationUptimeTimer.Stop();
        _albionGameProcessMonitor.GameStarted -= AlbionGameProcessMonitor_OnGameStarted;
        _albionGameProcessMonitor.GameStopped -= AlbionGameProcessMonitor_OnGameStopped;
        _albionGameProcessMonitor.Dispose();
        _systemTrayService.Dispose();
        _mainWindowViewModel.DisposeItemDetails();
        _mainWindowViewModel.CraftingBindings.DisposeLossExplorer();
        SettingsController.SetWindowSettings(windowStateForPersistence, Height, Width, Left, Top);
    }

    // WPF Loaded: runs once when the window is first shown.
    private void MainWindow_OnLoaded(object sender, EventArgs eventArgs)
    {
        Opened -= MainWindow_OnLoaded;

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

    private void ApplicationUptimeTimer_OnTick(object sender, EventArgs e)
    {
        UpdateApplicationUptime();
    }

    private void UpdateApplicationUptime()
    {
        ApplicationUptimeLabel.Content = AppStartup.ApplicationUptime.ToTimerString();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = AvaloniaWindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        _windowChromeController.ToggleMaximize();
    }

    private void CopyPartyToClipboard_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        var trackingController = ServiceLocator.Resolve<TrackingController>();
        trackingController?.EntityController?.CopyPartyToClipboard();
    }

    private void TatsDropDownOpenClose_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        _mainWindowViewModel?.SwitchStatsDropDownState();
    }
}
