// Avalonia port of Common/Controls/FontAwesomeImage.cs (itself a subclass of DSaladin FontAwesome.WPF's FontAwesomeImage):
// draws a Font Awesome 6 Free glyph, stretched uniformly into the control and filled with Foreground. The glyph comes from the
// embedded fonts (Assets/Fonts) through FontAwesomeGlyphs. Same XAML: <fa:FontAwesomeImage Icon="SolidBell" Foreground="..."
// Spin="True" SpinDuration="5" Width="16" Height="16"/>. Without Width/Height the icon is 16x16.
using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using DSaladin.FontAwesome.WPF;
using StatisticsAnalysisTool.Linux.Icons;

namespace StatisticsAnalysisTool.Common.Controls;

public sealed class FontAwesomeImage : Control
{
    private const double DefaultSize = 16;

    public static readonly StyledProperty<FontAwesomeIcon> IconProperty =
        AvaloniaProperty.Register<FontAwesomeImage, FontAwesomeIcon>(nameof(Icon));

    public static readonly StyledProperty<IBrush> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<FontAwesomeImage>();

    public static readonly StyledProperty<bool> SpinProperty =
        AvaloniaProperty.Register<FontAwesomeImage, bool>(nameof(Spin));

    public static readonly StyledProperty<double> SpinDurationProperty =
        AvaloniaProperty.Register<FontAwesomeImage, double>(nameof(SpinDuration), 4d, validate: value => value > 0);

    private readonly RotateTransform _spinTransform = new();
    private readonly Stopwatch _spinClock = new();
    private DispatcherTimer _spinTimer;
    private bool _attached;

    static FontAwesomeImage()
    {
        AffectsRender<FontAwesomeImage>(IconProperty, ForegroundProperty);
        SpinProperty.Changed.AddClassHandler<FontAwesomeImage>((image, _) => image.UpdateSpin());
        SpinDurationProperty.Changed.AddClassHandler<FontAwesomeImage>((image, _) => image.UpdateSpin());
    }

    public FontAwesomeImage()
    {
        RenderTransformOrigin = RelativePoint.Center;
        RenderTransform = _spinTransform;
    }

    public FontAwesomeIcon Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public IBrush Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public bool Spin
    {
        get => GetValue(SpinProperty);
        set => SetValue(SpinProperty, value);
    }

    public double SpinDuration
    {
        get => GetValue(SpinDurationProperty);
        set => SetValue(SpinDurationProperty, value);
    }

    internal double CurrentAngle => _spinTransform.Angle;

    /// <summary>The rotation after <paramref name="elapsed"/> when a full turn takes <paramref name="durationSeconds"/> seconds.</summary>
    internal static double SpinAngle(TimeSpan elapsed, double durationSeconds)
    {
        if (durationSeconds <= 0)
        {
            return 0;
        }

        return elapsed.TotalSeconds / durationSeconds % 1 * 360;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(Math.Min(DefaultSize, availableSize.Width), Math.Min(DefaultSize, availableSize.Height));
    }

    public override void Render(DrawingContext context)
    {
        var geometry = FontAwesomeGlyphs.CreateGeometry(Icon);
        var brush = Foreground;
        if (geometry == null || brush == null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var glyph = geometry.Bounds;
        if (glyph.Width <= 0 || glyph.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(Bounds.Width / glyph.Width, Bounds.Height / glyph.Height);
        var matrix = Matrix.CreateTranslation(-glyph.X, -glyph.Y)
                     * Matrix.CreateScale(scale, scale)
                     * Matrix.CreateTranslation((Bounds.Width - glyph.Width * scale) / 2, (Bounds.Height - glyph.Height * scale) / 2);
        using (context.PushTransform(matrix))
        {
            context.DrawGeometry(brush, null, geometry);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        UpdateSpin();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        UpdateSpin();
    }

    // WPF ran a RotateTransform animation; here a 60 Hz timer drives the same transform while the control is on screen.
    private void UpdateSpin()
    {
        _spinTimer?.Stop();
        _spinTransform.Angle = 0;
        if (!Spin || !_attached)
        {
            return;
        }

        _spinClock.Restart();
        _spinTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _spinTimer.Tick -= OnSpinTick;
        _spinTimer.Tick += OnSpinTick;
        _spinTimer.Start();
    }

    private void OnSpinTick(object sender, EventArgs e)
    {
        _spinTransform.Angle = SpinAngle(_spinClock.Elapsed, SpinDuration);
    }
}
