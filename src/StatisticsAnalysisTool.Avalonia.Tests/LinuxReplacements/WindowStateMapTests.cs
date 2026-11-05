using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;
using AvaloniaWindowState = Avalonia.Controls.WindowState;
using WpfWindowState = System.Windows.WindowState;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class WindowStateMapTests
{
    [TestCase(AvaloniaWindowState.Normal, WpfWindowState.Normal)]
    [TestCase(AvaloniaWindowState.Minimized, WpfWindowState.Minimized)]
    [TestCase(AvaloniaWindowState.Maximized, WpfWindowState.Maximized)]
    [TestCase(AvaloniaWindowState.FullScreen, WpfWindowState.Maximized)]
    public void ToWpf(AvaloniaWindowState state, WpfWindowState expected) => WindowStateMap.ToWpf(state).Should().Be(expected);

    [TestCase(WpfWindowState.Normal, AvaloniaWindowState.Normal)]
    [TestCase(WpfWindowState.Minimized, AvaloniaWindowState.Minimized)]
    [TestCase(WpfWindowState.Maximized, AvaloniaWindowState.Maximized)]
    public void ToAvalonia(WpfWindowState state, AvaloniaWindowState expected) => WindowStateMap.ToAvalonia(state).Should().Be(expected);
}
