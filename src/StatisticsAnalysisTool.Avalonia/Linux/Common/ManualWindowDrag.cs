// Fallback window move for compositors where the native move request (BeginMoveDrag) is rejected: the window follows the
// pointer by the screen-space delta since the press. Pure logic, the window wires it to pointer events.
using Avalonia;

namespace StatisticsAnalysisTool.Common;

public sealed class ManualWindowDrag
{
    private PixelPoint _pointerStart;
    private PixelPoint _windowStart;

    public bool IsActive { get; private set; }

    public void Begin(PixelPoint pointerOnScreen, PixelPoint windowPosition)
    {
        _pointerStart = pointerOnScreen;
        _windowStart = windowPosition;
        IsActive = true;
    }

    /// <summary>The new window position for the pointer, or null when no drag is running.</summary>
    public PixelPoint? Move(PixelPoint pointerOnScreen)
    {
        if (!IsActive)
        {
            return null;
        }

        return new PixelPoint(
            _windowStart.X + pointerOnScreen.X - _pointerStart.X,
            _windowStart.Y + pointerOnScreen.Y - _pointerStart.Y);
    }

    public void End() => IsActive = false;
}
