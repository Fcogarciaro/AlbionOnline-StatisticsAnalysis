using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using StatisticsAnalysisTool.Linux.Startup;

namespace StatisticsAnalysisTool.Avalonia;

public class LinuxApp : Application
{
    private AppStartup _startup;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Like WPF's async void OnStartup: RunAsync handles its own failures.
            _startup = new AppStartup(desktop);
            _ = _startup.RunAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
