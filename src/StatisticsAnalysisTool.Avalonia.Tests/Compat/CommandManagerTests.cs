using System.Windows.Input;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat;

public class CommandManagerTests
{
    [AvaloniaTest]
    public void InvalidateRequerySuggested_RaisesSubscribedHandlersOnTheUiThread()
    {
        var raised = 0;
        EventHandler handler = (_, _) => raised++;
        CommandManager.RequerySuggested += handler;

        CommandManager.InvalidateRequerySuggested();
        AvaloniaDispatcher.UIThread.RunJobs();
        CommandManager.RequerySuggested -= handler;
        CommandManager.InvalidateRequerySuggested();
        AvaloniaDispatcher.UIThread.RunJobs();

        raised.Should().Be(1);
    }
}
