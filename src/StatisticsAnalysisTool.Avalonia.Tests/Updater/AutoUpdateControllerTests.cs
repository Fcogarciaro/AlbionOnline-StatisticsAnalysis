using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Avalonia.Tests.Views;
using StatisticsAnalysisTool.Linux.Updater;
using StatisticsAnalysisTool.Updater;
using StatisticsAnalysisTool.ViewModels;

namespace StatisticsAnalysisTool.Avalonia.Tests.Updater;

[NonParallelizable]
public class AutoUpdateControllerTests
{
    private const string Json = """
    [ { "tag_name": "linux-v1.5.0", "name": "Linux 1.5.0", "body": "## Changes\n- faster startup\n* fixed crash\n\n",
        "html_url": "https://github.com/Fcogarciaro/AlbionOnline-StatisticsAnalysis/releases/tag/linux-v1.5.0",
        "draft": false, "prerelease": false, "published_at": "2026-05-01T10:00:00Z",
        "assets": [ { "name": "StatisticsAnalysisTool-linux-x64.tar.gz", "browser_download_url": "https://example.invalid/a.tar.gz" } ] } ]
    """;

    private readonly List<(string Message, bool IsError)> _messages = [];
    private readonly List<string> _opened = [];
    private UpdateWindowViewModel _presented;

    [SetUp]
    public void SetUp()
    {
        _messages.Clear();
        _opened.Clear();
        _presented = null;
        AutoUpdateController.ResetForTests();
        AutoUpdateController.CurrentVersion = () => new Version(1, 0, 0);
        AutoUpdateController.FetchReleasesJson = _ => Task.FromResult(Json);
        AutoUpdateController.ShowMessage = (message, _, isError) => _messages.Add((message, isError));
        AutoUpdateController.OpenUrl = url => { _opened.Add(url); return true; };
        AutoUpdateController.PresentUpdate = (viewModel, action) => { _presented = viewModel; _action = action; return Task.CompletedTask; };
    }

    private Func<Task<bool>> _action;

    [TearDown]
    public void TearDown() => AutoUpdateController.ResetForTests();

    [AvaloniaTest]
    public async Task ManualCheck_WithANewerRelease_FlagsTheUpdateAndOpensTheWindowWithTheReleaseNotes()
    {
        using var harness = new ViewHarness();
        var changes = new List<bool>();
        AutoUpdateController.UpdateAvailabilityChanged += changes.Add;
        try
        {
            await AutoUpdateController.CheckForUpdatesAsync();

            AutoUpdateController.IsUpdateAvailable.Should().BeTrue();
            changes.Should().Equal(true);
            _presented.Should().NotBeNull();
            _presented.VersionText.Should().Contain("1.5.0");
            _presented.HasPatchNotes.Should().BeTrue();
            _presented.ReleaseNoteGroups.Single().Sections.Single().Items.Should().Equal("Changes", "faster startup", "fixed crash");
        }
        finally
        {
            AutoUpdateController.UpdateAvailabilityChanged -= changes.Add;
        }
    }

    [AvaloniaTest]
    public async Task TheUpdateAction_OpensTheReleasePageAndReportsSuccess()
    {
        using var harness = new ViewHarness();
        await AutoUpdateController.CheckForUpdatesAsync();

        var result = await _action();

        result.Should().BeTrue();
        _opened.Should().Equal("https://github.com/Fcogarciaro/AlbionOnline-StatisticsAnalysis/releases/tag/linux-v1.5.0");
    }

    [AvaloniaTest]
    public async Task BackgroundCheck_FlagsTheUpdateSilently()
    {
        using var harness = new ViewHarness();

        await AutoUpdateController.CheckInBackgroundAsync();

        AutoUpdateController.IsUpdateAvailable.Should().BeTrue();
        _presented.Should().BeNull("the window only opens on a manual check or from the footer");
        _messages.Should().BeEmpty();

        await AutoUpdateController.ShowAvailableUpdateWindowAsync();
        _presented.Should().NotBeNull();
    }

    [AvaloniaTest]
    public async Task ManualCheck_WhenUpToDate_TellsTheUserAndClearsTheFlag()
    {
        using var harness = new ViewHarness();
        AutoUpdateController.CurrentVersion = () => new Version(1, 5, 0);

        await AutoUpdateController.CheckForUpdatesAsync();

        AutoUpdateController.IsUpdateAvailable.Should().BeFalse();
        _presented.Should().BeNull();
        _messages.Should().ContainSingle().Which.IsError.Should().BeFalse();
    }

    [AvaloniaTest]
    public async Task ManualCheck_WhenTheRequestFails_ReportsAnErrorAndDoesNotThrow()
    {
        using var harness = new ViewHarness();
        AutoUpdateController.FetchReleasesJson = _ => throw new HttpRequestException("offline");

        await AutoUpdateController.CheckForUpdatesAsync();

        AutoUpdateController.IsUpdateAvailable.Should().BeFalse();
        _messages.Should().ContainSingle().Which.IsError.Should().BeTrue();
    }

    [AvaloniaTest]
    public async Task BackgroundCheck_WhenTheRequestFails_StaysSilent()
    {
        using var harness = new ViewHarness();
        AutoUpdateController.FetchReleasesJson = _ => throw new HttpRequestException("offline");

        await AutoUpdateController.CheckInBackgroundAsync();

        AutoUpdateController.IsUpdateAvailable.Should().BeFalse();
        _messages.Should().BeEmpty();
    }

    [TestCase("## Changes\n- a\n* b\n\n  - c  \n", new[] { "Changes", "a", "b", "c" })]
    [TestCase("", new string[0])]
    [TestCase(null, new string[0])]
    [TestCase("plain\r\ntext", new[] { "plain", "text" })]
    public void ReleaseNotes_ToItems_StripsMarkdownMarkers(string body, string[] expected)
    {
        ReleaseNotes.ToItems(body).Should().Equal(expected);
    }
}
