// Avalonia replacement for SkiaSharp.Views.WPF SKElement: a control that raises PaintSurface with a canvas sized like the control
// (device independent units, the same size the WPF element reports). Drawing goes through an ICustomDrawOperation and the Skia API lease,
// so the handler code of the WPF views (DrawRoundRect, DrawText, ...) runs unchanged on the Avalonia Skia backend.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;

namespace StatisticsAnalysisTool.Common.Controls;

public sealed class SKPaintSurfaceEventArgs(SKCanvas canvas, SKImageInfo info) : EventArgs
{
    public SKCanvas Canvas { get; } = canvas;

    public SKImageInfo Info { get; } = info;
}

public class SKElement : Control
{
    public event EventHandler<SKPaintSurfaceEventArgs> PaintSurface;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        context.Custom(new SkiaDrawOperation(new Rect(0, 0, Bounds.Width, Bounds.Height), this));
    }

    protected virtual void OnPaintSurface(SKPaintSurfaceEventArgs e) => PaintSurface?.Invoke(this, e);

    private sealed class SkiaDrawOperation(Rect bounds, SKElement owner) : ICustomDrawOperation
    {
        public Rect Bounds { get; } = bounds;

        public void Dispose()
        {
        }

        public bool Equals(ICustomDrawOperation other) => false;

        public bool HitTest(Point p) => Bounds.Contains(p);

        public void Render(ImmediateDrawingContext context)
        {
            var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()?.Lease();
            if (lease == null)
            {
                return;
            }

            using (lease)
            {
                var info = new SKImageInfo((int) Math.Ceiling(Bounds.Width), (int) Math.Ceiling(Bounds.Height));
                // The WPF element drew into its own transparent bitmap that was composited over the parent, so Canvas.Clear(Transparent) kept the
                // backdrop. On the shared canvas Clear replaces pixels (and the lease clip is the whole parent area): clip to the element and
                // draw into a transparent layer that is composited when the handler returns.
                var canvas = lease.SkCanvas;
                canvas.Save();
                try
                {
                    canvas.ClipRect(new SKRect(0, 0, (float) Bounds.Width, (float) Bounds.Height));
                    canvas.SaveLayer();
                    owner.OnPaintSurface(new SKPaintSurfaceEventArgs(canvas, info));
                }
                finally
                {
                    canvas.Restore();
                    canvas.Restore();
                }
            }
        }
    }
}
