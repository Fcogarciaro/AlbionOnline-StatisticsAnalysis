// Avalonia port of Common/ProgressBarSmoother.cs. WPF animated RangeBase.Value with a DoubleAnimation (250 ms);
// Avalonia drives the same linear interpolation with a 60 Hz DispatcherTimer. A new SmoothValue restarts the
// animation from the current value (HandoffBehavior.SnapshotAndReplace).
using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace StatisticsAnalysisTool.Common;

public class ProgressBarSmoother
{
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(250);
    private static readonly ConditionalWeakTable<ProgressBar, Animation> Animations = new();

    /// <summary>Animation clock; tests replace it so intermediate values do not depend on how long the machine stalls.</summary>
    internal static TimeProvider Clock { get; set; } = TimeProvider.System;

    public static readonly AttachedProperty<double> SmoothValueProperty =
        AvaloniaProperty.RegisterAttached<ProgressBarSmoother, ProgressBar, double>("SmoothValue");

    static ProgressBarSmoother()
    {
        SmoothValueProperty.Changed.AddClassHandler<ProgressBar>(Changing);
    }

    public static double GetSmoothValue(ProgressBar obj) => obj.GetValue(SmoothValueProperty);

    public static void SetSmoothValue(ProgressBar obj, double value) => obj.SetValue(SmoothValueProperty, value);

    internal static double NormalizeValue(double value, double minimum, double maximum)
    {
        return double.IsNaN(value) || double.IsInfinity(value) ? minimum : Math.Clamp(value, minimum, maximum);
    }

    internal static double ValueAt(double from, double to, TimeSpan elapsed)
    {
        var progress = Math.Clamp(elapsed.TotalMilliseconds / Duration.TotalMilliseconds, 0d, 1d);
        return from + (to - from) * progress;
    }

    private static void Changing(ProgressBar progressBar, AvaloniaPropertyChangedEventArgs e)
    {
        var from = NormalizeValue(progressBar.Value, progressBar.Minimum, progressBar.Maximum);
        var to = NormalizeValue((double) e.NewValue!, progressBar.Minimum, progressBar.Maximum);
        if (Animations.TryGetValue(progressBar, out var running))
        {
            running.Stop();
        }

        var animation = new Animation(progressBar, from, to);
        Animations.AddOrUpdate(progressBar, animation);
        animation.Start();
    }

    private sealed class Animation
    {
        private readonly ProgressBar _progressBar;
        private readonly double _from;
        private readonly double _to;
        private readonly TimeProvider _clock = Clock;
        private long _startTimestamp;
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };

        public Animation(ProgressBar progressBar, double from, double to)
        {
            _progressBar = progressBar;
            _from = from;
            _to = to;
            _timer.Tick += OnTick;
        }

        public void Start()
        {
            _progressBar.Value = _from;
            _startTimestamp = _clock.GetTimestamp();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            var elapsed = _clock.GetElapsedTime(_startTimestamp);
            _progressBar.Value = ValueAt(_from, _to, elapsed);
            if (elapsed >= Duration)
            {
                Stop();
            }
        }
    }
}
