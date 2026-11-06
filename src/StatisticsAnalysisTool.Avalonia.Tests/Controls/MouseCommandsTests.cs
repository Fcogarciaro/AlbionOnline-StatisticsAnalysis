using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common;
using System.Windows.Input;

namespace StatisticsAnalysisTool.Avalonia.Tests.Controls;

[NonParallelizable]
public class MouseCommandsTests
{
    private sealed class Recorder : ICommand
    {
        public List<object> Calls { get; } = [];
        public event EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => Calls.Add(parameter);
    }

    private static (Window Window, Border Target) Host(Action<Border> configure)
    {
        var target = new Border { Width = 100, Height = 100, Background = global::Avalonia.Media.Brushes.Red };
        configure(target);
        var window = new Window { Width = 200, Height = 200, Content = target };
        window.Show();
        return (window, target);
    }

    [AvaloniaTest]
    public void LeftClick_ExecutesTheCommandWithItsParameterOnTheLeftButton()
    {
        var command = new Recorder();
        var (window, _) = Host(b => { MouseCommands.SetLeftClick(b, command); MouseCommands.SetCommandParameter(b, "p"); });

        window.MouseDown(new Point(50, 50), MouseButton.Left);
        window.MouseUp(new Point(50, 50), MouseButton.Left);
        window.MouseDown(new Point(50, 50), MouseButton.Right);
        window.MouseUp(new Point(50, 50), MouseButton.Right);

        command.Calls.Should().Equal("p");
    }

    [AvaloniaTest]
    public void LeftDoubleClick_ExecutesOnlyOnTheSecondPress()
    {
        var command = new Recorder();
        var (window, _) = Host(b => MouseCommands.SetLeftDoubleClick(b, command));

        window.MouseDown(new Point(50, 50), MouseButton.Left);
        window.MouseUp(new Point(50, 50), MouseButton.Left);
        command.Calls.Should().BeEmpty();
        window.MouseDown(new Point(50, 50), MouseButton.Left);
        window.MouseUp(new Point(50, 50), MouseButton.Left);

        command.Calls.Should().HaveCount(1);
    }

    [AvaloniaTest]
    public void ClearingTheCommandStopsExecution()
    {
        var command = new Recorder();
        var (window, target) = Host(b => MouseCommands.SetLeftClick(b, command));

        MouseCommands.SetLeftClick(target, null);
        window.MouseDown(new Point(50, 50), MouseButton.Left);

        command.Calls.Should().BeEmpty();
    }
}
