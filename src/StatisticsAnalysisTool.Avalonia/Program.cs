using System;
using Avalonia;

namespace StatisticsAnalysisTool.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<LinuxApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
