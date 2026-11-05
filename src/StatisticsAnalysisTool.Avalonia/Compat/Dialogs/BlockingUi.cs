// Helpers that give async Avalonia UI operations (dialogs, pickers) the synchronous shape WPF code expects:
// the caller blocks inside a nested dispatcher frame, so the UI keeps rendering and input keeps flowing.
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using AvaloniaApplication = Avalonia.Application;

namespace System.Windows;

internal static class BlockingUi
{
    /// <summary>Runs <paramref name="operation"/> on the UI thread and waits for it without freezing the UI.</summary>
    public static T Run<T>(Func<Task<T>> operation)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return Dispatcher.UIThread.Invoke(() => Run(operation));
        }

        var task = operation();
        if (!task.IsCompleted)
        {
            var frame = new DispatcherFrame();
            _ = task.ContinueWith(_ => Dispatcher.UIThread.Post(() => frame.Continue = false), TaskScheduler.Default);
            Dispatcher.UIThread.PushFrame(frame);
        }

        return task.GetAwaiter().GetResult();
    }

    /// <summary>The visible window dialogs and pickers should attach to: active window, else main window, else any.</summary>
    public static Avalonia.Controls.Window FindOwner()
    {
        if (AvaloniaApplication.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return null;
        }

        var visible = desktop.Windows.Where(w => w.IsVisible).ToList();
        return visible.FirstOrDefault(w => w.IsActive)
               ?? (desktop.MainWindow is { IsVisible: true } main ? main : null)
               ?? visible.LastOrDefault();
    }
}
