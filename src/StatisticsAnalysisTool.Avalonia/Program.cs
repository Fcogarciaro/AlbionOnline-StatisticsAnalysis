using System;
using Avalonia;

namespace StatisticsAnalysisTool.Avalonia;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // Bind the compat Dispatcher to the UI thread before any worker thread can touch it.
        _ = global::Avalonia.Threading.Dispatcher.UIThread;
        // Resolve libpcap.so.1 / libpcap.so.0.8 when no libpcap.so development symlink exists.
        StatisticsAnalysisTool.Linux.Capture.LibpcapLibrary.Install();
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<LinuxApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
