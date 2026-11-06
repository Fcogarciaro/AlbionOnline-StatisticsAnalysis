// Avalonia port of Common/ScrollViewerExtensions.cs (attached property AlwaysScrollToEnd).
// Behavior difference: the WPF code compared `Math.Abs(offset - scrollableHeight) < 0`, which is never true, so it only
// scrolled to the end once when the property was set. This port implements the evident intent: after the first scroll
// to the end the view keeps following new content for as long as the user has not scrolled away from the end, and the
// state is kept per ScrollViewer (WPF shared one static flag).
using System;
using Avalonia;
using Avalonia.Controls;

namespace StatisticsAnalysisTool.Common;

public class ScrollViewerExtensions
{
    public static readonly AttachedProperty<bool> AlwaysScrollToEndProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewerExtensions, ScrollViewer, bool>("AlwaysScrollToEnd");

    static ScrollViewerExtensions()
    {
        AlwaysScrollToEndProperty.Changed.AddClassHandler<ScrollViewer>(AlwaysScrollToEndChanged);
    }

    public static bool GetAlwaysScrollToEnd(ScrollViewer scroll)
    {
        ArgumentNullException.ThrowIfNull(scroll);
        return scroll.GetValue(AlwaysScrollToEndProperty);
    }

    public static void SetAlwaysScrollToEnd(ScrollViewer scroll, bool alwaysScrollToEnd)
    {
        ArgumentNullException.ThrowIfNull(scroll);
        scroll.SetValue(AlwaysScrollToEndProperty, alwaysScrollToEnd);
    }

    /// <summary>True when the bottom of the viewport touched the end of the content (within a pixel).</summary>
    internal static bool WasAtEnd(double offset, double viewport, double extent) => offset + viewport >= extent - 1;

    private static void AlwaysScrollToEndChanged(ScrollViewer scroll, AvaloniaPropertyChangedEventArgs e)
    {
        scroll.ScrollChanged -= ScrollChanged;
        if (e.NewValue is true)
        {
            scroll.ScrollToEnd();
            scroll.ScrollChanged += ScrollChanged;
        }
    }

    private static void ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scroll || e.ExtentDelta.Y == 0)
        {
            return;
        }

        var previousExtent = scroll.Extent.Height - e.ExtentDelta.Y;
        var previousOffset = scroll.Offset.Y - e.OffsetDelta.Y;
        var previousViewport = scroll.Viewport.Height - e.ViewportDelta.Y;
        if (WasAtEnd(previousOffset, previousViewport, previousExtent))
        {
            scroll.ScrollToEnd();
        }
    }
}
