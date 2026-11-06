using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.UserControls;
using StatisticsAnalysisTool.ViewModels;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class SettingsControlTests
{
    private string _root;
    private IDisposable _xdg;
    private BindingErrorLog _log;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("sat-settings").FullName;
        _xdg = XdgDirectories.UseForTests(XdgDirectories.Resolve(name => name switch
        {
            "XDG_CONFIG_HOME" => Path.Combine(_root, "config"),
            "XDG_DATA_HOME" => Path.Combine(_root, "data"),
            "XDG_CACHE_HOME" => Path.Combine(_root, "cache"),
            _ => _root
        }));
        _log = new BindingErrorLog();
    }

    [TearDown]
    public void TearDown()
    {
        _log.Dispose();
        _xdg.Dispose();
        Directory.Delete(_root, true);
    }

    private static Window Host(out SettingsControl control)
    {
        LocalizationController.Init().Should().BeTrue();
        if (!StatisticsAnalysisTool.Common.ServiceLocator.IsServiceInDictionary<MainWindowViewModel>())
        {
            StatisticsAnalysisTool.Common.ServiceLocator.Register<MainWindowViewModel>(new MainWindowViewModel());
        }

        control = new SettingsControl();
        var window = new Window { Width = 1100, Height = 800, Content = control };
        window.Show();
        return window;
    }

    [AvaloniaTest]
    public void Renders_AllCardsWithTheViewModelAndNoBindingErrors()
    {
        var window = Host(out var control);

        control.DataContext.Should().BeOfType<SettingsWindowViewModel>();
        var titles = control.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Theme?.TargetType == null && t.Text is { Length: > 0 }).Select(t => t.Text).ToList();
        control.GetVisualDescendants().OfType<CheckBox>().Count().Should().BeGreaterThan(8, "option checkboxes of window, startup and tracking");
        control.GetVisualDescendants().OfType<ComboBox>().Count().Should().BeGreaterThan(8);
        control.GetVisualDescendants().OfType<Button>().Should().Contain(b => b.Name == null && b.Content is string);
        _log.Errors.Should().BeEmpty();
        window.Close();
    }

    [AvaloniaTest]
    public void PacketProviders_AreRelabeledForLinux()
    {
        var window = Host(out var control);
        var viewModel = (SettingsWindowViewModel) control.DataContext!;
        var combo = control.GetVisualDescendants().OfType<ComboBox>().Single(c => ReferenceEquals(c.ItemsSource, viewModel.PacketProvider));

        combo.ItemCount.Should().Be(2);
        var labels = viewModel.PacketProvider
            .Select(item =>
            {
                var content = combo.ItemTemplate!.Build(item)!;
                content.DataContext = item;
                return ((TextBlock) content).Text;
            })
            .ToList();

        labels.Should().BeEquivalentTo("libpcap", "Sockets (mapped to libpcap)");
        window.Close();
    }

    [AvaloniaTest]
    public void BackupPath_ShowsTheValidationErrorAsTooltipAndRedBorder()
    {
        var window = Host(out var control);
        var box = control.FindControl<TextBox>("BackupStorageDirectoryPathTextBox")!;
        var viewModel = (SettingsWindowViewModel) control.DataContext!;

        box.Text = Path.Combine(_root, "does-not-exist");
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        box.Classes.Should().Contain("invalid");
        ToolTip.GetTip(box).Should().Be("Path does not exist.");
        viewModel.BackupStorageDirectoryPath.Should().Be(Path.Combine(_root, "does-not-exist"));

        box.Text = _root;
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        box.Classes.Should().NotContain("invalid");
        ToolTip.GetTip(box).Should().BeNull();
        window.Close();
    }

    [AvaloniaTest]
    public void TrackingStateButton_ExistsAndStartsUnchecked()
    {
        var window = Host(out var control);

        control.FindControl<ToggleButton>("TrackingStateButton")!.IsChecked.Should().BeFalse();
        window.Close();
    }
}
