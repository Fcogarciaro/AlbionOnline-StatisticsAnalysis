using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common.Controls;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.EventLogging;
using StatisticsAnalysisTool.EventLogging.Notification;
using StatisticsAnalysisTool.UserControls;
using System.Windows;
using System.Windows.Data;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class LoggingControlTests
{
    [AvaloniaTest]
    public void Renders_BothTabsTheFilterCardAndTheNotificationListWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new LoggingControl(), harness.MainViewModel, width: 1400);

        control.GetVisualDescendants().OfType<TabItem>().Should().HaveCountGreaterThanOrEqualTo(3, "logging, loot comparator and the notifications/stats tabs");
        control.GetVisualDescendants().OfType<ToggleButton>().Where(t => t.TemplatedParent == null).Should().NotBeEmpty();
        control.GetVisualDescendants().OfType<TextBox>().Where(t => t.TemplatedParent == null).Should().ContainSingle("the search box");
        control.GetVisualDescendants().OfType<FontAwesomeImage>().Should().NotBeEmpty();
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void NotificationList_VirtualizesThousandsOfEntries()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new LoggingControl(), harness.MainViewModel, width: 1400);
        var bindings = harness.MainViewModel.LoggingBindings;
        bindings.TrackingNotifications = [];
        bindings.GameLoggingCollectionView = CollectionViewSource.GetDefaultView(bindings.TrackingNotifications) as ListCollectionView;
        for (var i = 0; i < 5000; i++)
        {
            bindings.TrackingNotifications.Add(new TrackingNotification(DateTime.Now.AddSeconds(i), new TextFragment($"event {i}"), LoggingFilterType.Fame));
        }

        bindings.GameLoggingCollectionView.Refresh();
        Dispatcher.UIThread.RunJobs();

        var list = control.GetVisualDescendants().OfType<ListBox>().First(l => l.ItemsSource == bindings.GameLoggingCollectionView);
        var realized = list.GetVisualDescendants().OfType<ListBoxItem>().Count();
        realized.Should().BeGreaterThan(0).And.BeLessThan(200, "only the visible rows (plus a small cache) are realized");
        harness.Log.Errors.Should().BeEmpty();
        bindings.GameLoggingCollectionView = null;
        bindings.TrackingNotifications = [];
    }

    [AvaloniaTest]
    public void Notifications_RenderEveryFragmentKindWithItsIconsAndColors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new LoggingControl(), harness.MainViewModel, width: 1400);
        var bindings = harness.MainViewModel.LoggingBindings;
        bindings.TrackingNotifications = [];
        bindings.GameLoggingCollectionView = CollectionViewSource.GetDefaultView(bindings.TrackingNotifications) as ListCollectionView;
        var now = DateTime.Now;
        bindings.TrackingNotifications.Add(new TrackingNotification(now, new TextFragment("plain text"), LoggingFilterType.Fame));
        bindings.TrackingNotifications.Add(new TrackingNotification(now.AddSeconds(1),
            new FameNotificationFragment("You got", AttributeStatOperator.Plus, 10, 1000, "fame", PvpPveType.Pve, 100, 50, 10, true, 20, "end"), LoggingFilterType.Fame));
        bindings.TrackingNotifications.Add(new TrackingNotification(now.AddSeconds(2),
            new FameNotificationFragment("You lost", AttributeStatOperator.Minus, 10, 1000, "fame", PvpPveType.Pvp, 100, 50, 10, false, 0, "end"), LoggingFilterType.Fame));
        bindings.TrackingNotifications.Add(new TrackingNotification(now.AddSeconds(3), new CombatNotificationFragment("Alice", "Bob", 1234), LoggingFilterType.Fame));
        bindings.TrackingNotifications.Add(new TrackingNotification(now.AddSeconds(4),
            new KillNotificationFragment("Bob", "G1", "A1", "Alice", "", "", "1,000 silver"), LoggingFilterType.Fame));
        bindings.GameLoggingCollectionView.Refresh();
        Dispatcher.UIThread.RunJobs();

        var texts = control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty).ToList();
        texts.Should().Contain(t => t.StartsWith("plain text"));
        texts.Should().Contain(t => t.StartsWith("You got"));
        texts.Should().Contain(t => t.StartsWith("Alice"));
        texts.Should().Contain("+ ").And.Contain("- ", "the operator text follows AttributeStatOperator");
        control.GetVisualDescendants().OfType<Image>().Count(i => i.Source != null).Should().BeGreaterThanOrEqualTo(8, "fame, zone, crown, satchel icons and the skull");
        harness.Log.Errors.Should().BeEmpty();
        bindings.GameLoggingCollectionView = null;
        bindings.TrackingNotifications = [];
    }

    [AvaloniaTest]
    public void LootComparator_ListsLootingPlayersWithBadgesAndCopyRemoveButtons()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new LoggingControl(), harness.MainViewModel, width: 1400, height: 1000);
        var bindings = harness.MainViewModel.LoggingBindings;
        var player = new LootingPlayer { PlayerName = "Alice", PlayerGuild = "G" };
        player.LootedItems.Add(new LootedItem { ItemIndex = 1, Quantity = 3, Status = LootedItemStatus.Resolved });
        bindings.LootingPlayersCollectionView = CollectionViewSource.GetDefaultView(bindings.LootingPlayers) as ListCollectionView;
        bindings.LootingPlayers.Add(player);
        var tab = control.GetVisualDescendants().OfType<TabControl>().First();
        tab.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();

        control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().Contain("Alice (G)");
        control.GetVisualDescendants().OfType<Button>().Count(b => b.Command == bindings.RemoveLootingPlayerCommand).Should().Be(1);
        control.GetVisualDescendants().OfType<Button>().Single(b => b.Command == bindings.CopyLootingPlayerLogsCommand).CommandParameter.Should().Be(player);
        harness.Log.Errors.Should().BeEmpty();
        bindings.LootingPlayers.Clear();
        bindings.LootingPlayersCollectionView = null;
    }

    [AvaloniaTest]
    public void InfoToggle_ShowsAndHidesTheHelpPanel()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new LoggingControl(), harness.MainViewModel, width: 1400, height: 1000);
        var bindings = harness.MainViewModel.LoggingBindings;
        bindings.IsLootComparatorInfoPopupVisible.Should().Be(Visibility.Collapsed);

        bindings.ToggleLootComparatorInfoPopupVisibility();
        Dispatcher.UIThread.RunJobs();
        control.GetVisualDescendants().OfType<ScrollViewer>().Where(s => s.Margin.Top == 400).Single().IsVisible.Should().BeTrue();

        control.GetVisualDescendants().OfType<TextBox>().Single(t => t.TemplatedParent == null).Should().NotBeNull("the search box");
        harness.Log.Errors.Should().BeEmpty();
        bindings.ToggleLootComparatorInfoPopupVisibility();
    }

    [AvaloniaTest]
    public void StatsTab_ListsTheKillRankingWithTheKillerDetail()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new LoggingControl(), harness.MainViewModel, width: 1400, height: 1000);
        var stats = harness.MainViewModel.LoggingBindings.LootLoggerStats;
        stats.Reset();
        stats.RecordKillDeath("Bob", "Alice");
        stats.Refresh();
        var inner = control.GetVisualDescendants().OfType<TabControl>().Last(t => t.Items.Count == 2);
        inner.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        List<string> Texts() => control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty).ToList();
        for (var i = 0; i < 300 && !(Texts().Contains("Alice") && Texts().Contains("Bob")); i++)
        {
            Dispatcher.UIThread.RunJobs();
            System.Threading.Thread.Sleep(10);
        }

        var texts = Texts();
        texts.Should().Contain("Alice", "the most kills entry");
        texts.Should().Contain("Bob", "the most deaths entry");
        harness.Log.Errors.Should().BeEmpty();
        stats.Reset();
    }
}
