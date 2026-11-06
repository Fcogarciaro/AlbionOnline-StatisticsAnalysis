// Compatibility shim: System.Windows.Input.CommandManager. WPF re-queries commands on input events;
// here re-querying happens only when code (or a ported view) calls InvalidateRequerySuggested.
using System.Windows.Threading;

namespace System.Windows.Input;

public static class CommandManager
{
    public static event EventHandler RequerySuggested;

    public static void InvalidateRequerySuggested()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => RequerySuggested?.Invoke(null, EventArgs.Empty));
    }
}
