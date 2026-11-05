// Compatibility shim: mirrors the subset of System.Windows.Threading.Dispatcher used by the linked
// WPF logic and forwards to Avalonia's UI-thread dispatcher.
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;
using AvaloniaPriority = Avalonia.Threading.DispatcherPriority;

namespace System.Windows.Threading;

public enum DispatcherPriority
{
    Inactive = 0,
    SystemIdle = 1,
    ApplicationIdle = 2,
    ContextIdle = 3,
    Background = 4,
    Input = 5,
    Loaded = 6,
    Render = 7,
    DataBind = 8,
    Normal = 9,
    Send = 10
}

public sealed class Dispatcher
{
    public static Dispatcher CurrentDispatcher { get; } = new();

    private static AvaloniaPriority Map(DispatcherPriority priority) => priority switch
    {
        DispatcherPriority.Inactive or DispatcherPriority.SystemIdle => AvaloniaPriority.SystemIdle,
        DispatcherPriority.ApplicationIdle => AvaloniaPriority.ApplicationIdle,
        DispatcherPriority.ContextIdle => AvaloniaPriority.ContextIdle,
        DispatcherPriority.Background => AvaloniaPriority.Background,
        DispatcherPriority.Input => AvaloniaPriority.Input,
        DispatcherPriority.Loaded => AvaloniaPriority.Loaded,
        DispatcherPriority.Render => AvaloniaPriority.Render,
        DispatcherPriority.DataBind => AvaloniaPriority.DataBind,
        DispatcherPriority.Send => AvaloniaPriority.Send,
        _ => AvaloniaPriority.Normal
    };

    public bool CheckAccess() => AvaloniaDispatcher.UIThread.CheckAccess();

    public void VerifyAccess() => AvaloniaDispatcher.UIThread.VerifyAccess();

    public void Invoke(Action callback) => Invoke(callback, DispatcherPriority.Send);

    public void Invoke(Action callback, DispatcherPriority priority)
    {
        if (CheckAccess())
        {
            callback();
            return;
        }

        AvaloniaDispatcher.UIThread.Invoke(callback, Map(priority));
    }

    public T Invoke<T>(Func<T> callback) => Invoke(callback, DispatcherPriority.Send);

    public T Invoke<T>(Func<T> callback, DispatcherPriority priority)
    {
        return CheckAccess() ? callback() : AvaloniaDispatcher.UIThread.Invoke(callback, Map(priority));
    }

    public DispatcherOperation InvokeAsync(Action callback) => InvokeAsync(callback, DispatcherPriority.Normal);

    public DispatcherOperation InvokeAsync(Action callback, DispatcherPriority priority) =>
        InvokeAsync(callback, priority, CancellationToken.None);

    public DispatcherOperation InvokeAsync(Action callback, DispatcherPriority priority, CancellationToken cancellationToken) =>
        new(AvaloniaDispatcher.UIThread.InvokeAsync(callback, Map(priority), cancellationToken));

    public DispatcherOperation<T> InvokeAsync<T>(Func<T> callback) => InvokeAsync(callback, DispatcherPriority.Normal);

    public DispatcherOperation<T> InvokeAsync<T>(Func<T> callback, DispatcherPriority priority) =>
        InvokeAsync(callback, priority, CancellationToken.None);

    public DispatcherOperation<T> InvokeAsync<T>(Func<T> callback, DispatcherPriority priority, CancellationToken cancellationToken) =>
        new(AvaloniaDispatcher.UIThread.InvokeAsync(callback, Map(priority), cancellationToken));

    public DispatcherOperation BeginInvoke(Delegate method, params object[] args) =>
        BeginInvoke(DispatcherPriority.Normal, method, args);

    public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method, params object[] args) =>
        InvokeAsync(() => InvokeUnwrapped(method, args), priority);

    // Delegate.DynamicInvoke wraps exceptions in TargetInvocationException; WPF surfaces the original one.
    private static void InvokeUnwrapped(Delegate method, object[] args)
    {
        try
        {
            method.DynamicInvoke(args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }
}
