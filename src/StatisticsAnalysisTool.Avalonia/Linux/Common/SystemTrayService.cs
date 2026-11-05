// Linux replacement for Common/SystemTrayService.cs (WinForms NotifyIcon). Same public surface and menu: Open,
// Activate/Deactivate tracking, Exit, plus minimize-to-tray behaviour. The tray icon is an Avalonia TrayIcon
// (StatusNotifierItem on Linux desktops that provide one). Avalonia exposes no balloon-tip click event, so the
// first-hide hint is a notify-send desktop notification.
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using Serilog;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.Models.TranslationModel;
using StatisticsAnalysisTool.Network.Manager;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Threading.Tasks;
using AvaloniaWindow = Avalonia.Controls.Window;
using AvaloniaWindowState = Avalonia.Controls.WindowState;

namespace StatisticsAnalysisTool.Common;

public sealed class SystemTrayService : IDisposable
{
    private const string ApplicationName = "Statistics Analysis Tool";
    private const string IconUri = "avares://StatisticsAnalysisTool/Assets/sat-icon.ico";

    private readonly AvaloniaWindow _window;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly TrayIcon _trayIcon;
    private readonly NativeMenuItem _openMenuItem;
    private readonly NativeMenuItem _trackingMenuItem;
    private readonly NativeMenuItem _exitMenuItem;
    private AvaloniaWindowState _windowStateBeforeMinimizing;
    private bool _isWindowHidden;
    private bool _hasShownMinimizedNotification;
    private bool _isTrackingStateChanging;
    private bool _isDisposed;

    public SystemTrayService(AvaloniaWindow window, MainWindowViewModel mainWindowViewModel)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
        _windowStateBeforeMinimizing = window.WindowState == AvaloniaWindowState.Minimized ? AvaloniaWindowState.Normal : window.WindowState;

        _openMenuItem = new NativeMenuItem();
        _trackingMenuItem = new NativeMenuItem();
        _exitMenuItem = new NativeMenuItem();
        var menu = new NativeMenu();
        menu.Add(_openMenuItem);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(_trackingMenuItem);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(_exitMenuItem);
        menu.Opening += (_, _) => RefreshLocalizedText();

        _trayIcon = new TrayIcon
        {
            Icon = LoadApplicationIcon(),
            ToolTipText = ApplicationName,
            Menu = menu,
            IsVisible = false
        };

        RefreshLocalizedText();
        _window.PropertyChanged += Window_OnPropertyChanged;
        _openMenuItem.Click += OpenMenuItem_OnClick;
        _trackingMenuItem.Click += TrackingMenuItem_OnClick;
        _exitMenuItem.Click += ExitMenuItem_OnClick;
        _trayIcon.Clicked += TrayIcon_OnClicked;

        if (global::Avalonia.Application.Current != null)
        {
            TrayIcon.SetIcons(global::Avalonia.Application.Current, [_trayIcon]);
        }
    }

    public System.Windows.WindowState WindowStateForPersistence =>
        WindowStateMap.ToWpf(_isWindowHidden ? _windowStateBeforeMinimizing : _window.WindowState);

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _window.PropertyChanged -= Window_OnPropertyChanged;
        _openMenuItem.Click -= OpenMenuItem_OnClick;
        _trackingMenuItem.Click -= TrackingMenuItem_OnClick;
        _exitMenuItem.Click -= ExitMenuItem_OnClick;
        _trayIcon.Clicked -= TrayIcon_OnClicked;
        _trayIcon.IsVisible = false;
        if (global::Avalonia.Application.Current != null)
        {
            TrayIcon.SetIcons(global::Avalonia.Application.Current, []);
        }

        _trayIcon.Dispose();
    }

    private void Window_OnPropertyChanged(object sender, global::Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != AvaloniaWindow.WindowStateProperty)
        {
            return;
        }

        if (_window.WindowState != AvaloniaWindowState.Minimized)
        {
            _windowStateBeforeMinimizing = _window.WindowState;
            return;
        }

        if (!SettingsController.CurrentSettings.IsMinimizeToSystemTrayActive)
        {
            return;
        }

        HideWindowInSystemTray();
    }

    public void HideWindowInSystemTray(bool showNotification = true)
    {
        if (_isDisposed || _isWindowHidden)
        {
            return;
        }

        if (_window.WindowState != AvaloniaWindowState.Minimized)
        {
            _windowStateBeforeMinimizing = _window.WindowState;
        }

        _isWindowHidden = true;
        RefreshLocalizedText();
        _trayIcon.IsVisible = true;
        _window.Hide();

        if (!showNotification || _hasShownMinimizedNotification)
        {
            return;
        }

        _hasShownMinimizedNotification = true;
        DesktopNotifier.Send(NotificationKind.TrackingStatus, ApplicationName, SystemTrayTranslation.ApplicationContinuesToRunInSystemTray);
    }

    public void RestoreWindowFromSystemTray()
    {
        if (_isDisposed || !_isWindowHidden)
        {
            return;
        }

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(RestoreWindowFromSystemTray);
            return;
        }

        _isWindowHidden = false;
        _trayIcon.IsVisible = false;
        _window.WindowState = _windowStateBeforeMinimizing;
        _window.Show();
        _window.Activate();
    }

    private void ExitApplication()
    {
        if (_isDisposed)
        {
            return;
        }

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(ExitApplication);
            return;
        }

        _trayIcon.IsVisible = false;
        System.Windows.Application.Current.Shutdown();
    }

    private void RefreshLocalizedText()
    {
        _openMenuItem.Header = SystemTrayTranslation.OpenApplication;
        _trackingMenuItem.Header = _mainWindowViewModel.IsTrackingActive ? SystemTrayTranslation.DeactivateTracking : SystemTrayTranslation.ActivateTracking;
        _trackingMenuItem.IsEnabled = !_isTrackingStateChanging && ServiceLocator.IsServiceInDictionary<TrackingController>();
        _exitMenuItem.Header = SystemTrayTranslation.ExitApplication;
    }

    private async Task ToggleTrackingAsync()
    {
        if (_isTrackingStateChanging || !ServiceLocator.IsServiceInDictionary<TrackingController>())
        {
            return;
        }

        _isTrackingStateChanging = true;
        RefreshLocalizedText();

        try
        {
            var trackingController = ServiceLocator.Resolve<TrackingController>();
            if (_mainWindowViewModel.IsTrackingActive)
            {
                trackingController.StopTracking();
            }
            else
            {
                await trackingController.StartTrackingAsync();
            }
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Tracking could not be toggled from the system tray");
        }
        finally
        {
            _isTrackingStateChanging = false;
            RefreshLocalizedText();
        }
    }

    private void OpenMenuItem_OnClick(object sender, EventArgs eventArgs) => RestoreWindowFromSystemTray();

    private async void TrackingMenuItem_OnClick(object sender, EventArgs eventArgs) => await ToggleTrackingAsync();

    private void ExitMenuItem_OnClick(object sender, EventArgs eventArgs) => ExitApplication();

    // WinForms DoubleClick; StatusNotifierItem only reports a single activation.
    private void TrayIcon_OnClicked(object sender, EventArgs eventArgs) => RestoreWindowFromSystemTray();

    private static WindowIcon LoadApplicationIcon()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(IconUri));
            return new WindowIcon(stream);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "The application icon could not be loaded for the system tray");
            return null;
        }
    }
}
