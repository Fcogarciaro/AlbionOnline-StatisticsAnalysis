// Compatibility shim: mirrors the subset of System.Windows.Application used by the linked WPF logic
// (Current.Dispatcher, Shutdown). Resource lookups are intentionally not shimmed here.
using System.Windows.Threading;
using Avalonia.Controls.ApplicationLifetimes;

namespace System.Windows;

public class Application
{
    public static Application Current { get; } = new();

    public Dispatcher Dispatcher { get; } = Dispatcher.CurrentDispatcher;

    public void Shutdown()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }
}
