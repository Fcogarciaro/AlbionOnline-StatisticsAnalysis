// Compatibility shim: mirrors the subset of System.Windows.Threading.Dispatcher used by the linked
// WPF logic and forwards to Avalonia's UI-thread dispatcher.
using System.Threading.Tasks;
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

    public Task InvokeAsync(Action callback) => InvokeAsync(callback, DispatcherPriority.Normal);

    public Task InvokeAsync(Action callback, DispatcherPriority priority) =>
        AvaloniaDispatcher.UIThread.InvokeAsync(callback, Map(priority)).GetTask();

    public Task<T> InvokeAsync<T>(Func<T> callback) => InvokeAsync(callback, DispatcherPriority.Normal);

    public Task<T> InvokeAsync<T>(Func<T> callback, DispatcherPriority priority) =>
        AvaloniaDispatcher.UIThread.InvokeAsync(callback, Map(priority)).GetTask();

    public Task BeginInvoke(Delegate method, params object[] args) =>
        BeginInvoke(DispatcherPriority.Normal, method, args);

    public Task BeginInvoke(DispatcherPriority priority, Delegate method, params object[] args) =>
        AvaloniaDispatcher.UIThread.InvokeAsync(() => method.DynamicInvoke(args), Map(priority)).GetTask();
}
