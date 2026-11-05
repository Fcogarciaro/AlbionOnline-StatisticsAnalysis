// Avalonia port of Common/Controls/ResponsiveUniformGrid.cs: a UniformGrid whose column count follows the width of
// the widest visible child (as many columns as fit, never more than the visible children).
using System;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace StatisticsAnalysisTool.Common.Controls;

public sealed class ResponsiveUniformGrid : UniformGrid
{
    protected override Type StyleKeyOverride => typeof(UniformGrid);

    internal static int CalculateColumns(int visibleChildCount, double widestChild, double availableWidth)
    {
        var columns = Math.Max(1, visibleChildCount);
        if (!double.IsPositiveInfinity(availableWidth) && widestChild > 0)
        {
            columns = (int) Math.Max(1, Math.Min(columns, Math.Floor(availableWidth / widestChild)));
        }

        return columns;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var visibleChildCount = 0;
        var widestChild = 0d;
        var childAvailableSize = new Size(double.PositiveInfinity, availableSize.Height);

        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            child.Measure(childAvailableSize);
            widestChild = Math.Max(widestChild, child.DesiredSize.Width);
            visibleChildCount++;
        }

        SetCurrentValue(ColumnsProperty, CalculateColumns(visibleChildCount, widestChild, availableSize.Width));
        return base.MeasureOverride(availableSize);
    }
}
