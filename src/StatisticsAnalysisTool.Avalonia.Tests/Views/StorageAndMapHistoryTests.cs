using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using StatisticsAnalysisTool.Common;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common.Controls;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.Models.TranslationModel;
using StatisticsAnalysisTool.StorageHistory;
using StatisticsAnalysisTool.UserControls;
using StatisticsAnalysisTool.ViewModels;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class StorageAndMapHistoryTests
{
    [AvaloniaTest]
    public void StorageHistory_RendersFiltersTotalsAndTheDangerButtonWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new StorageHistoryControl(), harness.MainViewModel);

        control.GetVisualDescendants().OfType<ComboBox>().Should().HaveCount(2);
        control.GetVisualDescendants().OfType<GridSplitter>().Should().ContainSingle();
        control.GetVisualDescendants().OfType<FontAwesomeImage>().Should().ContainSingle("the filter icon");
        control.GetVisualDescendants().OfType<TextBox>().Where(t => t.TemplatedParent == null).Should().ContainSingle();
        control.GetVisualDescendants().OfType<Button>().Should().ContainSingle(b => b.Content is string);
        control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().Contain(
            [MainWindowTranslation.TotalBankValue, MainWindowTranslation.TotalChestValue, MainWindowTranslation.TotalWeight]);
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void StorageHistory_ListsVaultsContainerItemsAndSearchResultsWithCityColors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new StorageHistoryControl(), harness.MainViewModel);
        var vault = new Vault { Location = "Bank of Thetford", VaultContainer = [new VaultContainer { Name = "Chest 1", Guid = Guid.NewGuid() }] };
        var bindings = harness.MainViewModel.VaultBindings;
        bindings.Vaults = [vault];
        bindings.VaultContainerContent = [new ContainerItem { ItemIndex = 1, Quantity = 0 }, new ContainerItem { ItemIndex = 2, Quantity = 7 }];
        bindings.VaultSearchList.Add(new VaultSearchItem { Quantity = 3, VaultContainerName = "Chest 1", Location = "Bank of Thetford" });
        bindings.VaultSearchList.Add(new VaultSearchItem { Quantity = 1, VaultContainerName = "Chest 2", Location = "Somewhere else" });
        bindings.VaultSearchCollectionView.Refresh();
        Dispatcher.UIThread.RunJobs();

        var quantityBorders = control.GetVisualDescendants().OfType<Border>().Where(b => b.Child is Viewbox v && v.Width == 16).ToList();
        quantityBorders.Should().HaveCount(2);
        quantityBorders.Select(b => b.Classes.Contains("empty")).Should().Equal(true, false);

        var locationTexts = control.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Width == 200 && t.HorizontalAlignment == global::Avalonia.Layout.HorizontalAlignment.Right).ToList();
        locationTexts.Should().HaveCount(2);
        locationTexts.Select(t => t.Classes.Contains("thetford")).Should().Equal(true, false);
        ((ISolidColorBrush) locationTexts[0].Foreground!).Color.Should().Be(((ISolidColorBrush) control.FindResource("SolidColorBrush.City.Thetford")!).Color);
        harness.Log.Errors.Should().BeEmpty();
        bindings.Vaults = null;
        bindings.VaultContainerContent = null;
        bindings.VaultSearchList.Clear();
    }

    [AvaloniaTest]
    public void MapHistory_HasTheActionButtonsAndTheColumnHeaders()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new MapHistoryControl(), harness.MainViewModel);

        var buttons = control.GetVisualDescendants().OfType<Button>().Where(b => b.TemplatedParent == null).ToList();
        buttons.Should().HaveCount(2);
        control.FindControl<Button>("BtnCopySelectedMapHistoryToClipboard")!.IsEnabled.Should().BeFalse("nothing is selected");
        control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().Contain(
            [MainWindowTranslation.Time, MainWindowTranslation.Tier, MainWindowTranslation.Zone, MainWindowTranslation.Name, MainWindowTranslation.Type, MainWindowTranslation.Note]);
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void MapHistory_ClickingARowSelectsItAndEnablesTheCopyButton()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new MapHistoryControl(), harness.MainViewModel);
        var cluster = new StatisticsAnalysisTool.Cluster.ClusterInfo { Entered = new DateTime(2025, 1, 2, 3, 4, 5), Index = "3004" };
        harness.MainViewModel.EnteredCluster.Add(cluster);
        Dispatcher.UIThread.RunJobs();

        var rows = control.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("cluster-row-border")).ToList();
        rows.Should().ContainSingle();
        rows[0].Classes.Contains("selected").Should().BeFalse();
        rows[0].BorderThickness.Left.Should().Be(0);

        var clickArea = rows[0].GetVisualDescendants().OfType<StackPanel>().First(p => MouseCommands.GetLeftClick(p) != null);
        var point = clickArea.TranslatePoint(new Point(10, 10), harness.Window)!.Value;
        harness.Window.MouseDown(point, MouseButton.Left);
        harness.Window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        cluster.IsSelectedInMapHistory.Should().BeTrue();
        rows[0].Classes.Contains("selected").Should().BeTrue();
        rows[0].BorderThickness.Left.Should().Be(1);
        control.FindControl<Button>("BtnCopySelectedMapHistoryToClipboard")!.IsEnabled.Should().BeTrue();
        harness.Log.Errors.Should().BeEmpty();
        harness.MainViewModel.EnteredCluster.Clear();
    }
}
