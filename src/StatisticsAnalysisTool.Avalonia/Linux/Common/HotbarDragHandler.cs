// The secondary windows draw their own title bar ("Hotbar"). WPF: MouseDown -> DragMove(), and on some windows a double
// click toggles maximized/normal. Avalonia: PointerPressed -> BeginMoveDrag() with the same pointer-delta fallback as
// WindowChromeController when the compositor rejects the native move.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Serilog;
using System;
using AvaloniaWindowState = Avalonia.Controls.WindowState;

namespace StatisticsAnalysisTool.Common;

public sealed class HotbarDragHandler
{
    private readonly Window _window;
    private readonly bool _toggleMaximizeOnDoubleClick;
    private readonly ManualWindowDrag _manualDrag = new();

    public HotbarDragHandler(Window window, bool toggleMaximizeOnDoubleClick = false)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _toggleMaximizeOnDoubleClick = toggleMaximizeOnDoubleClick;
        _window.PointerMoved += Window_PointerMoved;
        _window.PointerReleased += Window_PointerReleased;
    }

    public void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(_window);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            if (_toggleMaximizeOnDoubleClick)
            {
                _window.WindowState = _window.WindowState == AvaloniaWindowState.Maximized
                    ? AvaloniaWindowState.Normal
                    : AvaloniaWindowState.Maximized;
            }

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

    private void Window_PointerReleased(object sender, PointerReleasedEventArgs e) => _manualDrag.End();
}
