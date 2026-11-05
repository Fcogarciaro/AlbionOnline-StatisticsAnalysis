// Compatibility shim: System.Windows.Threading.DispatcherTimer on top of Avalonia's timer.
using AvaloniaTimer = Avalonia.Threading.DispatcherTimer;

namespace System.Windows.Threading;

public class DispatcherTimer
{
    private readonly DispatcherPriority _priority;
    private TimeSpan _interval;
    private AvaloniaTimer _timer;

    public DispatcherTimer() : this(DispatcherPriority.Background)
    {
    }

    public DispatcherTimer(DispatcherPriority priority)
    {
        _priority = priority;
    }

    /// <summary>Like WPF, this overload starts the timer immediately.</summary>
    public DispatcherTimer(TimeSpan interval, DispatcherPriority priority, EventHandler callback, Dispatcher dispatcher)
        : this(priority)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _interval = interval;
        Tick += callback;
        Start();
    }

    public event EventHandler Tick;

    public bool IsEnabled
    {
        get => _timer != null;
        set
        {
            if (value)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
    }

    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _interval = value;
            if (IsEnabled)
            {
                Stop();
                Start();
            }
        }
    }

    public object Tag { get; set; }

    public void Start()
    {
        if (_timer != null)
        {
            return;
        }

        var timer = new AvaloniaTimer(_interval, Dispatcher.MapPriority(_priority), OnTick);
        _timer = timer;
        timer.Start();
    }

    public void Stop()
    {
        var timer = _timer;
        _timer = null;
        timer?.Stop();
    }

    private void OnTick(object sender, EventArgs e)
    {
        if (_timer == null)
        {
            return;
        }

        Tick?.Invoke(this, EventArgs.Empty);
    }
}
