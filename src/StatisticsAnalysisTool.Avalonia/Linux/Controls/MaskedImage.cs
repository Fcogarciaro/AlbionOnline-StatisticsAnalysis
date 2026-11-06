// WPF paints the navigation icons as a Rectangle filled with a color whose OpacityMask is an ImageBrush of the icon
// (the PNG is only a silhouette). Brushes cannot bind to the templated parent in Avalonia, so this control does the same
// thing as one element: <fa:MaskedImage Source="{TemplateBinding Tag, Converter=...}" Fill="..."/>.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace StatisticsAnalysisTool.Common.Controls;

public sealed class MaskedImage : Control
{
    public static readonly StyledProperty<global::Avalonia.Media.IImageBrushSource> SourceProperty =
        AvaloniaProperty.Register<MaskedImage, global::Avalonia.Media.IImageBrushSource>(nameof(Source));

    public static readonly StyledProperty<IBrush> FillProperty =
        AvaloniaProperty.Register<MaskedImage, IBrush>(nameof(Fill));

    static MaskedImage()
    {
        AffectsRender<MaskedImage>(SourceProperty, FillProperty);
    }

    public global::Avalonia.Media.IImageBrushSource Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public IBrush Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (Source == null || Fill == null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var bounds = new Rect(Bounds.Size);
        using (context.PushOpacityMask(new ImageBrush(Source) { Stretch = Stretch.Uniform }, bounds))
        {
            context.DrawRectangle(Fill, null, bounds);
        }
    }
}
