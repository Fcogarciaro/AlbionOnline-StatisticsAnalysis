using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.Linux.Startup;
using StatisticsAnalysisTool.UserControls;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;
using System.Text.RegularExpressions;
using AvaloniaWindowState = Avalonia.Controls.WindowState;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

// The real MainWindow with the real MainWindowViewModel, created by the real startup sequence.
[NonParallelizable]
public class MainWindowTests
{
    private string _root;
    private IDisposable _xdg;
    private BindingErrorLog _sink;
    private AppStartup _startup;
    private MainWindow _window;

    // The startup runs on the UI thread, so it is created from the test body ([SetUp] runs on another thread).
    private async Task OpenAsync()
    {
        _root = Directory.CreateTempSubdirectory("sat-mainwindow").FullName;
        _xdg = XdgDirectories.UseForTests(XdgDirectories.Resolve(name => name switch
        {
            "XDG_CONFIG_HOME" => Path.Combine(_root, "config"),
            "XDG_DATA_HOME" => Path.Combine(_root, "data"),
            "XDG_CACHE_HOME" => Path.Combine(_root, "cache"),
            _ => _root
        }));
        _sink = new BindingErrorLog();

        _startup = new AppStartup(new ClassicDesktopStyleApplicationLifetime(), (_, _, _, _) => Task.FromResult(true), _ => { }, installProcessHooks: false);
        await _startup.RunAsync();
        _window = (MainWindow) _startup.MainWindow;
        _window.Show();
    }

    [TearDown]
    public void TearDown()
    {
        _startup?.Cleanup();
        _window?.Close();
        _sink?.Dispose();
        _xdg?.Dispose();
        if (_root != null && Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [AvaloniaTest]
    public async Task Window_HasTheMainViewModelAndNoBindingErrors()
    {
        await OpenAsync();
        _window.DataContext.Should().BeOfType<MainWindowViewModel>();
        _window.Title.Should().Be("AlbionOnline - StatisticsAnalysisTool");
        _sink.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public async Task Navigation_HasEveryUpstreamTabInOrderWithLocalizedHeaders()
    {
        await OpenAsync();
        var tabs = _window.GetVisualDescendants().OfType<TabItem>().ToList();

        tabs.Should().HaveCount(16);
        tabs.Select(t => t.Tag).Should().Contain("pack://application:,,,/Assets/Navigation/dashboard.png")
            .And.Contain("pack://application:,,,/Assets/heart.png")
            .And.Contain("pack://application:,,,/Assets/Navigation/setting.png");
        tabs.Select(t => t.Header as string).Should().OnlyContain(h => !string.IsNullOrWhiteSpace(h));
        tabs[^1].Header.Should().Be(StatisticsAnalysisTool.Models.TranslationModel.MainWindowTranslation.Settings);
        _window.GetVisualDescendants().OfType<PortingPlaceholderControl>().Should().NotBeEmpty();
    }

    [AvaloniaTest]
    public async Task Header_ShowsUptimeStatusFooterAndBars()
    {
        await OpenAsync();
        var uptime = _window.FindControl<Label>("ApplicationUptimeLabel")!;
        Regex.IsMatch((string) uptime.Content!, @"^\d{2}:\d{2}:\d{2}$").Should().BeTrue();

        _window.GetVisualDescendants().OfType<FooterControl>().Should().ContainSingle();
        var version = _window.GetVisualDescendants().OfType<Label>().Single(l => l.Name == "VersionLabel");
        ((string) version.Content!).Should().StartWith("v");
        var viewModel = (MainWindowViewModel) _window.DataContext!;
        TestContext.Out.WriteLine($"VM error={viewModel.ErrorBarVisibility} '{viewModel.ErrorBarText}' warn={viewModel.WarningBarVisibility} info={viewModel.InformationBarVisibility}");
        _window.GetVisualDescendants().OfType<ErrorBarControl>().Single().IsVisible
            .Should().Be(viewModel.ErrorBarVisibility == System.Windows.Visibility.Visible);
        _window.GetVisualDescendants().OfType<WarningBarControl>().Single().IsVisible
            .Should().Be(viewModel.WarningBarVisibility == System.Windows.Visibility.Visible);
        _window.GetVisualDescendants().OfType<InformationBarControl>().Single().IsVisible
            .Should().Be(viewModel.InformationBarVisibility == System.Windows.Visibility.Visible);
        _sink.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public async Task ErrorBar_FollowsTheViewModelAndTheCloseButtonClearsIt()
    {
        await OpenAsync();
        var viewModel = (MainWindowViewModel) _window.DataContext!;
        var bar = _window.GetVisualDescendants().OfType<ErrorBarControl>().Single();

        viewModel.SetErrorBar(System.Windows.Visibility.Visible, "Something broke");

        bar.IsVisible.Should().BeTrue();
        bar.ErrorText.Should().Be("Something broke");
        bar.FindDescendantOfType<Button>()!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        viewModel.ErrorBarVisibility.Should().Be(System.Windows.Visibility.Collapsed);
        bar.IsVisible.Should().BeFalse();
    }

    [AvaloniaTest]
    public async Task MaximizeButton_TogglesTheWindowStateAndTheResizeHandles()
    {
        await OpenAsync();
        var button = _window.FindControl<Button>("MaximizedButton")!;
        var handles = _window.FindControl<Control>("ResizeHandles")!;
        handles.IsVisible.Should().BeTrue();

        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        _window.WindowState.Should().Be(AvaloniaWindowState.Maximized);
        button.Content.Should().Be(2);
        handles.IsVisible.Should().BeFalse("a maximized window cannot be resized");

        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        _window.WindowState.Should().Be(AvaloniaWindowState.Normal);
        handles.IsVisible.Should().BeTrue();
    }

    [AvaloniaTest]
    public async Task MinimizeButton_MinimizesTheWindow()
    {
        await OpenAsync();
        _window.FindControl<Button>("MinimizeButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        _window.WindowState.Should().Be(AvaloniaWindowState.Minimized);
    }

    [AvaloniaTest]
    public async Task CollapsingTheNavigationShowsTheIconsInsteadOfTheTexts()
    {
        await OpenAsync();
        var viewModel = (MainWindowViewModel) _window.DataContext!;
        var tab = _window.GetVisualDescendants().OfType<TabItem>().First();
        tab.GetVisualDescendants().OfType<Label>().Single(l => l.Name == "Text").IsVisible.Should().BeTrue();

        viewModel.IsNavigationMenuOpen = false;

        tab.GetVisualDescendants().OfType<Label>().Single(l => l.Name == "Text").IsVisible.Should().BeFalse();
        tab.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "Icon").IsVisible.Should().BeTrue();
        tab.GetVisualDescendants().OfType<Grid>().First().Width.Should().BeLessThan(190, "NavigationMenuWidth shrinks to the icon column");
        viewModel.IsNavigationMenuOpen = true;
    }

    [AvaloniaTest]
    public async Task StatsDropDown_TogglesFromTheArrowAndShowsTheIcons()
    {
        await OpenAsync();
        var viewModel = (MainWindowViewModel) _window.DataContext!;
        var before = viewModel.StatsDropDownVisibility;

        viewModel.SwitchStatsDropDownState();

        viewModel.StatsDropDownVisibility.Should().NotBe(before);
        _sink.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public async Task HeaderPointerPress_BeginsAMoveWithoutThrowingInTheHeadlessPlatform()
    {
        await OpenAsync();
        var header = _window.FindControl<Grid>("Hotbar")!;

        var act = () => _window.MouseDown(new global::Avalonia.Point(200, 20), MouseButton.Left);

        act.Should().NotThrow();
        header.Should().NotBeNull();
        _window.MouseUp(new global::Avalonia.Point(200, 20), MouseButton.Left);
    }
}
