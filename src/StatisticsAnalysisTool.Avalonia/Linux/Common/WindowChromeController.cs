// Linux replacement for Common/WindowChromeController.cs (WPF: WindowInteropHelper, Forms.Screen, DragMove).
// Same constructor and members. Differences: the title bar drag asks the compositor for a move (BeginMoveDrag) and falls back
// to moving the window by the pointer delta when that request is rejected (some Wayland setups); the maximized window
// takes the working area from the window manager, so MaxHeight is not set; WindowState changes are observed through the
// Avalonia property change instead of the StateChanged event. Keep in sync with the upstream file.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Serilog;
using System;
using System.Windows;
using AvaloniaWindowState = Avalonia.Controls.WindowState;

namespace StatisticsAnalysisTool.Common;

public sealed class WindowChromeController
{
    private const int RestoreButtonContent = 2;
    private const int MaximizeButtonContent = 1;

    private readonly System.Windows.Window _window;
    private readonly Button _maximizeButton;
    private readonly ResizeMode _restoredResizeMode;
    private readonly ResizeMode? _maximizedResizeMode;
    private readonly bool _centerOnRestore;
    private readonly bool _topmostOnMaximize;
    private readonly ManualWindowDrag _manualDrag = new();
    private bool _isMaximized;

    public WindowChromeController(
        System.Windows.Window window,
        Button maximizeButton,
        ResizeMode restoredResizeMode,
        ResizeMode? maximizedResizeMode = null,
        bool centerOnRestore = false,
        bool topmostOnMaximize = false)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _maximizeButton = maximizeButton ?? throw new ArgumentNullException(nameof(maximizeButton));
        _restoredResizeMode = restoredResizeMode;
        _maximizedResizeMode = maximizedResizeMode;
        _centerOnRestore = centerOnRestore;
        _topmostOnMaximize = topmostOnMaximize;
        _isMaximized = _window.WindowState == AvaloniaWindowState.Maximized;

        UpdateMaximizeButtonContent();
        _window.PropertyChanged += Window_PropertyChanged;
        _window.PointerMoved += Window_PointerMoved;
        _window.PointerReleased += Window_PointerReleased;
    }

    public void DragMoveOnMouseDown(PointerPressedEventArgs e)
    {
        if (e is null)
        {
            return;
        }

        var point = e.GetCurrentPoint(_window);
        if (!point.Properties.IsLeftButtonPressed || e.ClickCount != 1)
        {
            return;
        }

        try
        {
            _window.BeginMoveDrag(e);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            Log.Debug(ex, "Native window move was rejected; moving the window by the pointer delta.");
            _manualDrag.Begin(_window.PointToScreen(point.Position), _window.Position);
        }
    }

    public void ToggleMaximizeOnDoubleClick(PointerPressedEventArgs e)
    {
        if (e is null)
        {
            return;
        }

        if (!e.GetCurrentPoint(_window).Properties.IsLeftButtonPressed || e.ClickCount != 2)
        {
            return;
        }

        ToggleMaximize();
        e.Handled = true;
    }

    public void ToggleMaximize()
    {
        if (_isMaximized)
        {
            Restore();
            return;
        }

        Maximize();
    }

    private void Maximize()
    {
        if (_maximizedResizeMode.HasValue)
        {
            _window.ResizeMode = _maximizedResizeMode.Value;
        }

        if (_topmostOnMaximize)
        {
            _window.Topmost = true;
        }

        _window.WindowState = AvaloniaWindowState.Maximized;
        _isMaximized = true;
        UpdateMaximizeButtonContent();
    }

    private void Restore()
    {
        _window.WindowState = AvaloniaWindowState.Normal;
        _window.ResizeMode = _restoredResizeMode;
        _isMaximized = false;

        if (_topmostOnMaximize)
        {
            _window.Topmost = false;
        }

        if (_centerOnRestore)
        {
            Utilities.CenterWindowOnScreen(_window);
        }

        UpdateMaximizeButtonContent();
    }

    private void Window_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != global::Avalonia.Controls.Window.WindowStateProperty)
        {
            return;
        }

        _isMaximized = _window.WindowState == AvaloniaWindowState.Maximized;
        UpdateMaximizeButtonContent();
    }

    private void Window_PointerMoved(object sender, PointerEventArgs e)
    {
        if (!_manualDrag.IsActive)
        {
            return;
        }

        var position = _manualDrag.Move(_window.PointToScreen(e.GetPosition(_window)));
        if (position.HasValue)
        {
            _window.Position = position.Value;
        }
    }

    private void Window_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        _manualDrag.End();
    }

    private void UpdateMaximizeButtonContent()
    {
        _maximizeButton.Content = _isMaximized
            ? RestoreButtonContent
            : MaximizeButtonContent;
    }
}
