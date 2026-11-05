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
    private StatisticsAnalysisTool.Network.Manager.TrackingController _trackingController;

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

    /// <summary>
    /// Registers a real TrackingController for the lifetime of the harness (views whose handlers call ServiceLocator.Resolve need it, as in the app).
    /// It is removed again on dispose so other tests keep seeing the unregistered state.
    /// </summary>
    public StatisticsAnalysisTool.Network.Manager.TrackingController UseTrackingController()
    {
        _trackingController ??= new StatisticsAnalysisTool.Network.Manager.TrackingController(MainViewModel);
        StatisticsAnalysisTool.Common.ServiceLocator.Register<StatisticsAnalysisTool.Network.Manager.TrackingController>(_trackingController);
        return _trackingController;
    }

    public Window Window { get; private set; }

    public MainWindowViewModel MainViewModel => StatisticsAnalysisTool.Common.ServiceLocator.Resolve<MainWindowViewModel>();

    /// <summary>Shows the control; <paramref name="dataContext"/> is the inherited one (null keeps the control's own).</summary>
    public T Show<T>(T control, object dataContext = null, double width = 1100, double height = 800) where T : Control
    {
        if (dataContext != null)
        {
            control.DataContext = dataContext;
        }

        Window = new Window { Width = width, Height = height, Content = control };
        Window.Show();
        Dispatcher.UIThread.RunJobs();
        return control;
    }

    public void Dispose()
    {
        if (_trackingController != null)
        {
            var services = (System.Collections.IDictionary) typeof(StatisticsAnalysisTool.Common.ServiceLocator)
                .GetField("Services", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
            services.Remove(typeof(StatisticsAnalysisTool.Network.Manager.TrackingController));
        }

        Window?.Close();
        Log.Dispose();
        _xdg.Dispose();
        Directory.Delete(_root, true);
    }
}
