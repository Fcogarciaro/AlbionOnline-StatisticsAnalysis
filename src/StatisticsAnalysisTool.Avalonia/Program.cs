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

        // Upstream has no guard; two processes on one install would share one data directory (see PORTING.md).
        using var instanceLock = StatisticsAnalysisTool.Linux.Startup.SingleInstanceLock.TryAcquire(
            StatisticsAnalysisTool.Linux.Startup.SingleInstanceLock.ResolveDirectory(
                Environment.GetEnvironmentVariable,
                StatisticsAnalysisTool.Linux.Platform.XdgDirectories.Current.CacheRoot),
            StatisticsAnalysisTool.Common.AppInstance.InstanceId);
        if (instanceLock == null)
        {
            Console.Error.WriteLine("Statistics Analysis Tool is already running (same installation). Exiting.");
            return 1;
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<LinuxApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
