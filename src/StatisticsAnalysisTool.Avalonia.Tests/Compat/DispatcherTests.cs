using System.Reflection;
using System.Windows.Threading;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat;

public class DispatcherTests
{
    private static Dispatcher Sut => Dispatcher.CurrentDispatcher;

    [AvaloniaTest]
    public void InvokeAsync_Action_ReturnsOperationWithTaskThatCompletesAfterPump()
    {
        var ran = false;

        DispatcherOperation operation = Sut.InvokeAsync(() => ran = true);

        operation.Status.Should().Be(DispatcherOperationStatus.Pending);
        ran.Should().BeFalse("WPF InvokeAsync queues even on the UI thread");
        AvaloniaDispatcher.UIThread.RunJobs();
        operation.Task.IsCompletedSuccessfully.Should().BeTrue();
        operation.Status.Should().Be(DispatcherOperationStatus.Completed);
        ran.Should().BeTrue();
    }

    [AvaloniaTest]
    public void InvokeAsync_Func_ExposesResultAndTypedTask()
    {
        DispatcherOperation<int> operation = Sut.InvokeAsync(() => 42);

        AvaloniaDispatcher.UIThread.RunJobs();

        operation.Result.Should().Be(42);
        operation.Task.Result.Should().Be(42);
    }

    [AvaloniaTest]
    public async Task InvokeAsync_IsAwaitableDirectly()
    {
        var value = Sut.InvokeAsync(() => "x");
        AvaloniaDispatcher.UIThread.RunJobs();

        (await value).Should().Be("x");
        await Sut.InvokeAsync(() => { });
    }

    [AvaloniaTest]
    public void InvokeAsync_WithCancellationToken_AcceptsToken()
    {
        var operation = Sut.InvokeAsync(() => 1, DispatcherPriority.Background, CancellationToken.None);

        AvaloniaDispatcher.UIThread.RunJobs();

        operation.Result.Should().Be(1);
    }

    [AvaloniaTest]
    public void InvokeAsync_PropagatesExceptionThroughTask()
    {
        Action thrower = () => throw new InvalidOperationException("boom");
        var operation = Sut.InvokeAsync(thrower);

        AvaloniaDispatcher.UIThread.RunJobs();

        operation.Task.IsFaulted.Should().BeTrue();
        operation.Task.Exception!.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [AvaloniaTest]
    public void BeginInvoke_UnwrapsTargetInvocationException()
    {
        Action thrower = () => throw new InvalidOperationException("boom");

        var operation = Sut.BeginInvoke(thrower);

        AvaloniaDispatcher.UIThread.RunJobs();

        operation.Task.IsFaulted.Should().BeTrue();
        var inner = operation.Task.Exception!.InnerException;
        inner.Should().BeOfType<InvalidOperationException>();
        inner.Should().NotBeOfType<TargetInvocationException>();
    }

    [AvaloniaTest]
    public void BeginInvoke_PassesArguments()
    {
        var captured = 0;
        Action<int> action = v => captured = v;

        var operation = Sut.BeginInvoke(action, 7);
        AvaloniaDispatcher.UIThread.RunJobs();

        operation.Task.IsCompletedSuccessfully.Should().BeTrue();
        captured.Should().Be(7);
    }

    [AvaloniaTest]
    public void Invoke_OnUiThread_RunsInline()
    {
        Sut.Invoke(() => 5).Should().Be(5);
    }

    [AvaloniaTest]
    public void Wait_BlocksUntilCompleted_WhenPumpedFromAnotherThread()
    {
        var operation = Sut.InvokeAsync(() => 9);
        var pump = Task.Run(() => { Thread.Sleep(50); });
        pump.Wait();
        AvaloniaDispatcher.UIThread.RunJobs();

        operation.Wait();

        operation.Status.Should().Be(DispatcherOperationStatus.Completed);
    }
}
