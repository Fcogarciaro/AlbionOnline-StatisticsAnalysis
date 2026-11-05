using Avalonia.Controls;
using Avalonia.Threading;
using FluentAssertions;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.ViewModels;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

/// <summary>Hosts one real user control in a window with the real localization and the real main view model, and collects binding errors.</summary>
internal sealed class ViewHarness : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("sat-view").FullName;
    private readonly IDisposable _xdg;

    public ViewHarness()
    {
        _xdg = XdgDirectories.UseForTests(XdgDirectories.Resolve(name => name switch
        {
            "XDG_CONFIG_HOME" => Path.Combine(_root, "config"),
            "XDG_DATA_HOME" => Path.Combine(_root, "data"),
            "XDG_CACHE_HOME" => Path.Combine(_root, "cache"),
            _ => _root
        }));
        Log = new BindingErrorLog();
        LocalizationController.Init().Should().BeTrue();
        if (!StatisticsAnalysisTool.Common.ServiceLocator.IsServiceInDictionary<MainWindowViewModel>())
        {
            StatisticsAnalysisTool.Common.ServiceLocator.Register<MainWindowViewModel>(new MainWindowViewModel());
        }
    }

    public BindingErrorLog Log { get; }

    public Window Window { get; private set; }

    public MainWindowViewModel MainViewModel => StatisticsAnalysisTool.Common.ServiceLocator.Resolve<MainWindowViewModel>();

    /// <summary>Shows the control; <paramref name="dataContext"/> is the inherited one (null keeps the control's own).</summary>
    public T Show<T>(T control, object dataContext = null) where T : Control
    {
        if (dataContext != null)
        {
            control.DataContext = dataContext;
        }

        Window = new Window { Width = 1100, Height = 800, Content = control };
        Window.Show();
        Dispatcher.UIThread.RunJobs();
        return control;
    }

    public void Dispose()
    {
        Window?.Close();
        Log.Dispose();
        _xdg.Dispose();
        Directory.Delete(_root, true);
    }
}
