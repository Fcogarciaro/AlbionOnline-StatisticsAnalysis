using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.Linux.Startup;

namespace StatisticsAnalysisTool.Avalonia.Tests.Startup;

public class AppStartupTests
{
    private string _root;
    private IDisposable _xdg;
    private readonly List<string> _logs = [];

    [SetUp]
    public void SetUp()
    {
        Serilog.Log.Logger = new Serilog.LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(new ListSink(_logs)).CreateLogger();
        _root = Directory.CreateTempSubdirectory("sat-startup").FullName;
        _xdg = XdgDirectories.UseForTests(XdgDirectories.Resolve(name => name switch
        {
            "XDG_CONFIG_HOME" => Path.Combine(_root, "config"),
            "XDG_DATA_HOME" => Path.Combine(_root, "data"),
            "XDG_CACHE_HOME" => Path.Combine(_root, "cache"),
            _ => _root
        }));
    }

    [TearDown]
    public void TearDown()
    {
        _xdg.Dispose();
        Directory.Delete(_root, true);
    }

    [AvaloniaTest]
    public async Task GameDataFailure_ShutsDownEarly_BeforeAnyWindowOfTheMainFlowExists()
    {
        var lifetime = new FakeLifetime();
        var startup = new AppStartup(lifetime, (_, _, _, _) => Task.FromResult(false), (_, _) => { }, installProcessHooks: false);

        await startup.RunAsync();

        startup.IsEarlyShutdown.Should().BeTrue();
        lifetime.ShutdownCalls.Should().Be(1);
        lifetime.ShutdownMode.Should().Be(ShutdownMode.OnExplicitShutdown, "the mode only becomes OnMainWindowClose after the game data step");
        startup.MainWindow.Should().BeNull();
    }

    [AvaloniaTest]
    public async Task FullSequence_CreatesTheMainWindowWithTheRealViewModel_ThenCleansUpOnce()
    {
        var lifetime = new FakeLifetime();
        var infoDialogs = new List<(string, string)>();
        var progress = new List<double>();
        var startup = new AppStartup(
            lifetime,
            (_, report, _, _) =>
            {
                report(10, "fake game data");
                progress.Add(10);
                return Task.FromResult(true);
            },
            (title, text) => infoDialogs.Add((title, text)),
            installProcessHooks: false);

        await startup.RunAsync();

        startup.IsEarlyShutdown.Should().BeFalse(string.Join("\n", _logs));
        lifetime.ShutdownMode.Should().Be(ShutdownMode.OnMainWindowClose);
        lifetime.MainWindow.Should().BeSameAs(startup.MainWindow);
        startup.MainWindow.DataContext.Should().BeOfType<StatisticsAnalysisTool.ViewModels.MainWindowViewModel>();
        ServiceLocator.IsServiceInDictionary<StatisticsAnalysisTool.Network.Manager.TrackingController>().Should().BeTrue();
        infoDialogs.Should().ContainSingle("the capture info dialog is shown on the first start only");

        startup.Cleanup();
        startup.Cleanup();
        startup.MainWindow.Close();
    }

    private sealed class ListSink(List<string> logs) : Serilog.Core.ILogEventSink
    {
        public void Emit(Serilog.Events.LogEvent logEvent) => logs.Add(logEvent.RenderMessage() + (logEvent.Exception == null ? string.Empty : " " + logEvent.Exception));
    }

    // The lifetime interfaces cannot be implemented by user code, so a real (never started) lifetime is used and
    // Shutdown() is observed through its Exit event.
    private sealed class FakeLifetime : ClassicDesktopStyleApplicationLifetime
    {
        public FakeLifetime() => Exit += (_, _) => ShutdownCalls++;

        public int ShutdownCalls { get; private set; }
    }
}
