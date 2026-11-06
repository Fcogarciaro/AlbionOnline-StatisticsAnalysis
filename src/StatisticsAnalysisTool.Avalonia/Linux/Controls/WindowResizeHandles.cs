// Resize handles for borderless windows with ResizeMode CanResizeWithGrip: eight invisible borders plus the grip at the
// bottom right. Place it as the last child of the window's root grid; it is only visible while the window can be resized.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace StatisticsAnalysisTool.Common.Controls;

public sealed class WindowResizeHandles : Grid
{
    private Window _window;

    public WindowResizeHandles()
    {
        ZIndex = 100;
        Add(WindowEdge.North, HorizontalAlignment.Stretch, VerticalAlignment.Top, 0, 3, new Thickness(8, 0), StandardCursorType.SizeNorthSouth);
        Add(WindowEdge.South, HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 0, 3, new Thickness(8, 0), StandardCursorType.SizeNorthSouth);
        Add(WindowEdge.West, HorizontalAlignment.Left, VerticalAlignment.Stretch, 3, 0, new Thickness(0, 8), StandardCursorType.SizeWestEast);
        Add(WindowEdge.East, HorizontalAlignment.Right, VerticalAlignment.Stretch, 3, 0, new Thickness(0, 8), StandardCursorType.SizeWestEast);
        Add(WindowEdge.NorthWest, HorizontalAlignment.Left, VerticalAlignment.Top, 8, 8, default, StandardCursorType.TopLeftCorner);
        Add(WindowEdge.NorthEast, HorizontalAlignment.Right, VerticalAlignment.Top, 8, 8, default, StandardCursorType.TopRightCorner);
        Add(WindowEdge.SouthWest, HorizontalAlignment.Left, VerticalAlignment.Bottom, 8, 8, default, StandardCursorType.BottomLeftCorner);
        var grip = Add(WindowEdge.SouthEast, HorizontalAlignment.Right, VerticalAlignment.Bottom, 14, 14, default, StandardCursorType.BottomRightCorner);
        grip.Child = new global::Avalonia.Controls.Shapes.Path
        {
            Data = Geometry.Parse("M12,2 L2,12 M12,6 L6,12 M12,10 L10,12"),
            Stroke = new SolidColorBrush(Color.Parse("#FF8A9AA6")),
            StrokeThickness = 1,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (this.GetVisualRoot() is Window window)
        {
            _window = window;
            _window.PropertyChanged += Window_PropertyChanged;
            UpdateVisibility();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_window != null)
        {
            _window.PropertyChanged -= Window_PropertyChanged;
            _window = null;
        }
    }

    private void Window_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.CanResizeProperty || e.Property == Window.WindowStateProperty)
        {
            UpdateVisibility();
        }
    }

    // A maximized or minimized window cannot be resized from its edges.
    private void UpdateVisibility() => IsVisible = _window is { CanResize: true, WindowState: WindowState.Normal };

    private Border Add(WindowEdge edge, HorizontalAlignment horizontal, VerticalAlignment vertical, double width, double height, Thickness margin, StandardCursorType cursor)
    {
        var border = new Border
        {
            Background = Brushes.Transparent,
            HorizontalAlignment = horizontal,
            VerticalAlignment = vertical,
            Margin = margin,
            Cursor = new Cursor(cursor),
            Tag = edge
        };
        if (width > 0)
        {
            border.Width = width;
        }

        if (height > 0)
        {
            border.Height = height;
        }

        border.PointerPressed += (_, e) =>
        {
            if (this.GetVisualRoot() is Window window && e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
            {
                window.BeginResizeDrag(edge, e);
                e.Handled = true;
            }
        };
        Children.Add(border);
        return border;
    }
}
