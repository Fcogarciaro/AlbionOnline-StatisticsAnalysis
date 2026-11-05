using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common.Controls;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.Models.BindingModel;
using StatisticsAnalysisTool.UserControls;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class PlayerInformationControlTests
{
    private static PlayerModeInformationModel Model(string id) => new()
    {
        ServerName = "Europe",
        Timestamp = new DateTime(2025, 3, 4, 5, 6, 7, DateTimeKind.Utc),
        GameInfoPlayers = new GameInfoPlayersResponse
        {
            Id = id,
            Name = "Alice",
            GuildName = "Guild",
            KillFame = 1_500_000,
            DeathFame = 20_000,
            FameRatio = 75.5
        }
    };

    [AvaloniaTest]
    public void Renders_TheSearchHeaderWithTwoTabsAndNoBindingErrorsBeforeAnyData()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new PlayerInformationControl(), harness.MainViewModel, 1300, 900);

        control.GetVisualDescendants().OfType<TabItem>().Should().HaveCount(2);
        control.FindControl<TextBox>("PlayerSearchTextBox").Should().NotBeNull();
        control.GetVisualDescendants().OfType<ComboBox>().Should().ContainSingle("the server");
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void HeaderTabs_AreBoundTwoWayToTheSelectedTab()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new PlayerInformationControl(), harness.MainViewModel, 1300, 900);
        var bindings = harness.MainViewModel.PlayerInformationBindings;
        var tabs = control.GetVisualDescendants().OfType<TabControl>().Single();

        bindings.SelectedPlayerTabIndex = 1;
        Dispatcher.UIThread.RunJobs();
        tabs.SelectedIndex.Should().Be(1);

        tabs.SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();
        bindings.SelectedPlayerTabIndex.Should().Be(0);
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void Profile_ShowsNameStatisticsAndTheApiBlockForTheSearchedPlayer()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new PlayerInformationControl(), harness.MainViewModel, 1300, 900);
        var bindings = harness.MainViewModel.PlayerInformationBindings;
        bindings.PlayerModeInformation = Model("abc");
        bindings.SelectedPlayerTabIndex = 0;
        Dispatcher.UIThread.RunJobs();

        var texts = control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        texts.Should().Contain(["Alice", "abc", "Guild", "Europe"]);
        texts.Should().Contain("75.50", "the fame ratio uses N2");
        texts.Should().Contain(t => t != null && t.StartsWith("1.5") && t.EndsWith("M"), "the kill fame is a short number");
        texts.Should().Contain(PlayerModeTranslation.GuildName);
        control.GetVisualDescendants().OfType<UrlImage>().Should().HaveCount(2, "avatar and avatar ring");
        harness.Log.Errors.Should().BeEmpty();
        bindings.PlayerModeInformation = null;
    }

    [AvaloniaTest]
    public void Profile_ShowsNothingForAnEmptyModelAndTheLocalPlayerOnlyOnItsTab()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new PlayerInformationControl(), harness.MainViewModel, 1300, 900);
        var bindings = harness.MainViewModel.PlayerInformationBindings;
        bindings.PlayerModeInformation = new PlayerModeInformationModel();
        bindings.PlayerModeInformationLocal = Model("local");
        bindings.SelectedPlayerTabIndex = 0;
        Dispatcher.UIThread.RunJobs();

        control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().NotContain("Alice");
        var hosts = control.GetVisualChildren().SelectMany(c => c.GetVisualDescendants()).OfType<ContentControl>().Where(c => c.Content is PlayerModeInformationModel).ToList();
        hosts.Select(h => h.IsVisible).Should().Equal(true, false);

        bindings.SelectedPlayerTabIndex = 1;
        Dispatcher.UIThread.RunJobs();
        hosts.Select(h => h.IsVisible).Should().Equal(false, true);
        control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().Contain("Alice");
        harness.Log.Errors.Should().BeEmpty();
        bindings.PlayerModeInformation = null;
        bindings.PlayerModeInformationLocal = null;
    }

    [AvaloniaTest]
    public void SearchResultPopup_OpensBelowTheSearchBoxAndListsTheMatchesByNameAndGuild()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new PlayerInformationControl(), harness.MainViewModel, 1300, 900);
        var bindings = harness.MainViewModel.PlayerInformationBindings;
        var popup = control.GetLogicalDescendants().OfType<Popup>().Single();
        popup.IsOpen.Should().BeFalse();

        bindings.ListBoxUserSearchItems.Add(new PlayerInformationBindings.PlayerSearchStruct { Name = "Bob", Value = new SearchPlayerResponse { Id = "b", Name = "Bob", GuildName = "Builders" } });
        bindings.IsSearchResultOpen = true;
        Dispatcher.UIThread.RunJobs();

        popup.IsOpen.Should().BeTrue();
        var list = ((Border) popup.Child!).Child as ListBox;
        list.Should().NotBeNull();
        list!.ItemCount.Should().Be(1);
        list.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().Contain(["Bob", "Builders"]);
        harness.Log.Errors.Should().BeEmpty();

        bindings.IsSearchResultOpen = false;
        bindings.ListBoxUserSearchItems.Clear();
        Dispatcher.UIThread.RunJobs();
        popup.IsOpen.Should().BeFalse();
    }
}
