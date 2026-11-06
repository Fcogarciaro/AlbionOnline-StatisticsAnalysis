using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Capture;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;
using System.Collections.ObjectModel;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class GuideAndUpdateWindowTests
{
    private string _root;
    private IDisposable _xdg;
    private BindingErrorLog _log;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("sat-guide").FullName;
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

    private static CaptureEnvironment Missing => new(false, "/opt/sat/StatisticsAnalysisTool", "/opt/sat/StatisticsAnalysisTool.dll");

    private static FirstStartGuideWindow OpenGuide(CaptureEnvironment environment)
    {
        LocalizationController.Init().Should().BeTrue();
        var window = new FirstStartGuideWindow(environment, detectGameFolder: false);
        window.Show();
        return window;
    }

    [AvaloniaTest]
    public void FirstStartGuide_ShowsTheSetcapGuidanceWhenTheProcessCannotCapture()
    {
        var window = OpenGuide(Missing);

        var panel = window.FindControl<Border>("CapturePermissionPanel")!;
        panel.IsVisible.Should().BeTrue();
        window.FindControl<TextBlock>("CapturePermissionText")!.Text.Should()
            .Contain("sudo setcap cap_net_raw,cap_net_admin=eip /opt/sat/StatisticsAnalysisTool");
        _log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void FirstStartGuide_HidesTheGuidanceWhenCapable()
    {
        var window = OpenGuide(Missing with { IsCapable = true });

        window.FindControl<Border>("CapturePermissionPanel")!.IsVisible.Should().BeFalse();
    }

    [AvaloniaTest]
    public void FirstStartGuide_OffersLibpcapAndMapsSocketsToItWithoutNpcapTexts()
    {
        var window = OpenGuide(Missing);

        window.FindControl<RadioButton>("NpcapOption")!.Content.Should().Be("libpcap");
        window.FindControl<RadioButton>("SocketsOption")!.Content.Should().Be("Sockets (mapped to libpcap)");
        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Where(t => t != null);
        texts.Should().NotContain(t => t.Contains("npcap", StringComparison.OrdinalIgnoreCase));
        window.FindControl<TextBlock>("CapabilitiesLink")!.Text.Should().Be("About Linux capabilities");
    }

    [AvaloniaTest]
    public void FirstStartGuide_StartsOnTheLanguageStepWithTheLanguageList()
    {
        var window = OpenGuide(Missing);

        window.FindControl<Control>("LanguageStep")!.IsVisible.Should().BeTrue();
        window.FindControl<Control>("GameFolderStep")!.IsVisible.Should().BeFalse();
        window.FindControl<ComboBox>("LanguageComboBox")!.ItemCount.Should().BeGreaterThan(0);
        window.GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Ellipse>().Count(e => e.Classes.Contains("step")).Should().BeGreaterThan(3);
    }

    [AvaloniaTest]
    public void FirstStartGuide_CloseReturnsFalse()
    {
        var window = OpenGuide(Missing);

        window.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "CloseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        window.DialogResult.Should().BeFalse();
    }

    private static UpdateWindowViewModel UpdateViewModel(bool notes) => new(
        "Software Update", "A new version is available.", "Version 9.9 is out", "Release 9.9", "v9.9.0", "2026-11-05", notes,
        new ObservableCollection<UpdateReleaseNoteGroupViewModel>
        {
            new("9.9", "v9.9.0", "2026-11-05", new ObservableCollection<UpdateNoteSectionViewModel>
            {
                new("Fixes", System.Windows.Media.Brushes.White, new ObservableCollection<string> { "one", "two" })
            })
        },
        "Remind me later", "Download");

    [AvaloniaTest]
    public async Task UpdateWindow_ShowsReleaseNotesAndInstallResultClosesWithTrue()
    {
        var window = new UpdateWindow(UpdateViewModel(true), () => Task.FromResult(true));
        window.Show();

        window.Height.Should().Be(620);
        window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().Contain(["Release 9.9", "v9.9.0", "Fixes"]);
        window.GetVisualDescendants().OfType<TextBlock>().SelectMany(t => t.Inlines?.OfType<Run>() ?? []).Select(r => r.Text).Should().Contain("one");

        window.GetVisualDescendants().OfType<Button>().Single(b => (b.Content as string) == "Download").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(50);
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        window.DialogResult.Should().BeTrue();
        _log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void UpdateWindow_RemindLaterReturnsFalseAndTheSmallLayoutHidesTheNotes()
    {
        var window = new UpdateWindow(UpdateViewModel(false), null);
        window.Show();

        window.Height.Should().Be(360);
        window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Text == "Patch Notes").Should().OnlyContain(t => !t.IsEffectivelyVisible);
        window.GetVisualDescendants().OfType<Button>().Single(b => (b.Content as string) == "Remind me later").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        window.DialogResult.Should().BeFalse();
    }
}
