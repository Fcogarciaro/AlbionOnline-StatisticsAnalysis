using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Cluster;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Crafting;
using StatisticsAnalysisTool.Dungeon;
using StatisticsAnalysisTool.Dungeon.Models;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.UserControls;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class DungeonLossExplorerTests
{
    [AvaloniaTest]
    public void DungeonControl_RendersTheFiltersAnalyticsAndRunListWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new DungeonControl(), harness.MainViewModel, 1500, 1000);

        control.GetVisualDescendants().OfType<GridSplitter>().Should().ContainSingle();
        control.GetVisualDescendants().OfType<Button>().Where(b => b.TemplatedParent == null).Should().HaveCountGreaterThanOrEqualTo(3);
        control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().NotContain(t => t != null && t.Contains("Not yet ported"));
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void DungeonControl_ShowsOneEntryPerRunWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new DungeonControl(), harness.MainViewModel, 1500, 1000);
        var runList = control.GetVisualDescendants().OfType<ItemsControl>().Single(i => ReferenceEquals(i.ItemsSource, harness.MainViewModel.DungeonBindings.DungeonsCollectionView));
        runList.ItemsSource = new DungeonBaseFragment[]
        {
            new RandomDungeonFragment(Guid.NewGuid(), MapType.RandomDungeon, DungeonMode.Solo, "3004"),
            new HellGateFragment(Guid.NewGuid(), MapType.HellGate, DungeonMode.HellGate, "3004")
        };
        Dispatcher.UIThread.RunJobs();

        runList.GetVisualDescendants().OfType<ContentPresenter>().Count(c => c.Content is DungeonBaseFragment).Should().Be(2);
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void StatsTemplates_RenderTheirViewModelWithoutBindingErrors()
    {
        (string Key, Type ViewModel)[] cases =
        [
        ("Stats.Total.Template", typeof(StatsTotal)),
        ("Stats.Mists.Template", typeof(StatsMists)),
        ("Stats.MistsDungeon.Template", typeof(StatsMistsDungeon)),
        ("Stats.Solo.Template", typeof(StatsSolo)),
        ("Stats.Standard.Template", typeof(StatsStandard)),
        ("Stats.Avalonian.Template", typeof(StatsAvalonian)),
        ("Stats.Expedition.Template", typeof(StatsExpedition)),
        ("Stats.Corrupted.Template", typeof(StatsCorrupted)),
        ("Stats.HellGate.Template", typeof(StatsHellGate)),
        ("Stats.AbyssalDepths.Template", typeof(StatsAbyssalDepths)),
        ("Dungeon.Stats.Template", typeof(DungeonStats)),
        ("DungeonStatsFilterTemplate", typeof(DungeonStatsFilter)),
        ("DungeonOptionsObjectTemplate", typeof(DungeonOptionsObject))
        ];
        foreach (var (key, viewModel) in cases)
        {
            using var harness = new ViewHarness();
            var host = new ContentControl { Content = viewModel == typeof(DungeonStatsFilter) ? harness.MainViewModel.DungeonBindings.DungeonStatsFilter : Activator.CreateInstance(viewModel, true) };
            host.ContentTemplate = (global::Avalonia.Controls.Templates.IDataTemplate) global::Avalonia.Application.Current!.FindResource(key)!;
            harness.Show(host, null, 1200, 900);
            Dispatcher.UIThread.RunJobs();

            host.GetVisualDescendants().OfType<TextBlock>().Should().NotBeEmpty(key);
            harness.Log.Errors.Should().BeEmpty(key);
        }
    }

    [AvaloniaTest]
    public void LossExplorerControl_ShowsTheCardsOfTheEquipmentTabWithoutBindingErrors()
    {
        var settings = SettingsController.CurrentSettings;
        var previous = settings.LossExplorer;
        settings.LossExplorer = true;
        try
        {
            using var harness = new ViewHarness();
            var explorer = harness.MainViewModel.CraftingBindings.LossExplorer;
            explorer.Should().NotBeNull();
            explorer.EquipmentItems.Add(new LossExplorerItemRow(new LossExplorerCachedItem { ItemUniqueName = "T4_BAG", QualityLevel = 2, UnitValue = 1000 }, 3));
            var control = harness.Show(new LossExplorerControl(), harness.MainViewModel, 1500, 900);
            Dispatcher.UIThread.RunJobs();

            control.GetVisualDescendants().OfType<TabItem>().Should().HaveCount(3);
            control.GetVisualDescendants().OfType<ComboBox>().Where(c => c.TemplatedParent == null).Should().HaveCount(4);
            control.GetVisualDescendants().OfType<ItemsRepeater>().Should().NotBeEmpty();
            harness.Log.Errors.Should().BeEmpty();
        }
        finally
        {
            settings.LossExplorer = previous;
        }
    }
}
