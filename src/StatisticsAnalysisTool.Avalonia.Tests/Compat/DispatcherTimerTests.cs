using System.Windows.Threading;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat;

public class DispatcherTimerTests
{
    private static Task PumpFor(TimeSpan duration) => Task.Delay(duration);

    [AvaloniaTest]
    public async Task New_IsDisabledWithWpfDefaultInterval()
    {
        var timer = new DispatcherTimer();

        timer.IsEnabled.Should().BeFalse();
        timer.Interval.Should().Be(TimeSpan.Zero);
    }

    [AvaloniaTest]
    public async Task StartAndStop_ToggleIsEnabled()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };

        timer.Start();
        timer.IsEnabled.Should().BeTrue();
        timer.Stop();
        timer.IsEnabled.Should().BeFalse();
    }

    [AvaloniaTest]
    public async Task Tick_FiresRepeatedlyWhileEnabled_AndStopsAfterStop()
    {
        var ticks = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
        timer.Tick += (_, _) => ticks++;

        timer.Start();
        await PumpFor(TimeSpan.FromMilliseconds(250));
        timer.Stop();
        var afterStop = ticks;
        await PumpFor(TimeSpan.FromMilliseconds(100));

        afterStop.Should().BeGreaterThan(1);
        ticks.Should().Be(afterStop);
    }

    [AvaloniaTest]
    public async Task Tick_CanStopItselfFromHandler()
    {
        var ticks = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (s, _) =>
        {
            ticks++;
            ((DispatcherTimer) s!).Stop();
        };

        timer.Start();
        await PumpFor(TimeSpan.FromMilliseconds(150));

        ticks.Should().Be(1);
    }

    [AvaloniaTest]
    public async Task Constructor_WithCallback_StartsEnabledLikeWpf()
    {
        var ticks = 0;

        var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Background, (_, _) => ticks++, Dispatcher.CurrentDispatcher);
        await PumpFor(TimeSpan.FromMilliseconds(120));
        timer.Stop();

        ticks.Should().BeGreaterThan(0);
    }

    [AvaloniaTest]
    public async Task IntervalChange_WhileRunning_AppliesNewInterval()
    {
        var ticks = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
        timer.Tick += (_, _) => ticks++;
        timer.Start();

        timer.Interval = TimeSpan.FromMilliseconds(10);
        await PumpFor(TimeSpan.FromMilliseconds(150));
        timer.Stop();

        ticks.Should().BeGreaterThan(0);
        timer.Interval.Should().Be(TimeSpan.FromMilliseconds(10));
    }

    [AvaloniaTest]
    public async Task Tick_Unsubscribed_DoesNotFire()
    {
        var ticks = 0;
        EventHandler handler = (_, _) => ticks++;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += handler;
        timer.Tick -= handler;

        timer.Start();
        await PumpFor(TimeSpan.FromMilliseconds(80));
        timer.Stop();

        ticks.Should().Be(0);
    }
}
