using Avalonia;
using Avalonia.Headless;
using StatisticsAnalysisTool.Avalonia.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace StatisticsAnalysisTool.Avalonia.Tests;

public class TestApp : Application
{
}

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
