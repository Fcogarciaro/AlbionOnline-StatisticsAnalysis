using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Skia;
using Avalonia.Themes.Fluent;
using StatisticsAnalysisTool.Avalonia.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace StatisticsAnalysisTool.Avalonia.Tests;

public class TestApp : Application
{
    // Same style stack as LinuxApp (App.axaml), so tests see the real theme resources.
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude((Uri) null) { Source = new Uri("avares://StatisticsAnalysisTool/Styles/Theme.axaml") });
    }
}

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
