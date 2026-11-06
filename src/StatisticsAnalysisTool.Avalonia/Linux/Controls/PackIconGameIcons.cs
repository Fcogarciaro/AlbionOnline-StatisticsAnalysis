// Avalonia equivalent of MahApps.Metro.IconPacks.PackIconGameIcons (only the Kind values in PackIconGameIconsKind):
// <iconPacks:PackIconGameIcons Kind="Info" Width="24" Height="24" Foreground="..."/> with
// xmlns:iconPacks="clr-namespace:MahApps.Metro.IconPacks". The 512x512 view box is stretched uniformly into the control;
// like MahApps' default style the icon is 26x26 when no size is given.
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using StatisticsAnalysisTool.Linux.Icons;

namespace MahApps.Metro.IconPacks;

public sealed class PackIconGameIcons : Control
{
    private const double DefaultSize = 26;

    public static readonly StyledProperty<PackIconGameIconsKind> KindProperty =
        AvaloniaProperty.Register<PackIconGameIcons, PackIconGameIconsKind>(nameof(Kind));

    public static readonly StyledProperty<IBrush> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<PackIconGameIcons>();

    static PackIconGameIcons()
    {
        AffectsRender<PackIconGameIcons>(KindProperty, ForegroundProperty);
    }

    public PackIconGameIconsKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public IBrush Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(Math.Min(DefaultSize, availableSize.Width), Math.Min(DefaultSize, availableSize.Height));
    }

    public override void Render(DrawingContext context)
    {
        var geometry = GameIconsData.CreateGeometry(Kind);
        var brush = Foreground;
        if (geometry == null || brush == null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(Bounds.Width, Bounds.Height) / GameIconsData.ViewBoxSize;
        var matrix = Matrix.CreateScale(scale, scale)
                     * Matrix.CreateTranslation((Bounds.Width - GameIconsData.ViewBoxSize * scale) / 2, (Bounds.Height - GameIconsData.ViewBoxSize * scale) / 2);
        using (context.PushTransform(matrix))
        {
            context.DrawGeometry(brush, null, geometry);
        }
    }
}
