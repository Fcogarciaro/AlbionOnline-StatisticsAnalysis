// Maps Avalonia window states to the WPF enum persisted by the linked settings code.
using AvaloniaWindowState = Avalonia.Controls.WindowState;
using WpfWindowState = System.Windows.WindowState;

namespace StatisticsAnalysisTool.Linux.Platform;

internal static class WindowStateMap
{
    public static WpfWindowState ToWpf(AvaloniaWindowState state) => state switch
    {
        AvaloniaWindowState.Minimized => WpfWindowState.Minimized,
        AvaloniaWindowState.Maximized or AvaloniaWindowState.FullScreen => WpfWindowState.Maximized,
        _ => WpfWindowState.Normal
    };

    public static AvaloniaWindowState ToAvalonia(WpfWindowState state) => state switch
    {
        WpfWindowState.Minimized => AvaloniaWindowState.Minimized,
        WpfWindowState.Maximized => AvaloniaWindowState.Maximized,
        _ => AvaloniaWindowState.Normal
    };
}
