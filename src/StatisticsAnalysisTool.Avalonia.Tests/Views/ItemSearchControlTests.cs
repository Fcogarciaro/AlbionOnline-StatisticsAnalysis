using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common.Controls;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.Models.TranslationModel;
using StatisticsAnalysisTool.UserControls;
using System.Windows;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class ItemSearchControlTests
{
    [AvaloniaTest]
    public void Renders_FiltersGridCountersAndDetailsSlotWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new ItemSearchControl(), harness.MainViewModel, 1300, 700);

        control.GetVisualDescendants().OfType<ComboBox>().Where(c => c.TemplatedParent == null).Should().HaveCount(6, "three subcategories, shop category, tier and level");
        control.GetVisualDescendants().OfType<CheckBox>().Where(c => c.Content is string).Should().HaveCount(2);
        var grid = control.GetVisualDescendants().OfType<DataGrid>().Single();
        grid.Columns.Should().HaveCount(3);
        grid.CanUserResizeColumns.Should().BeFalse();
        control.GetVisualDescendants().OfType<ItemSearchImageCounterControl>().Should().ContainSingle();
        control.GetVisualDescendants().OfType<PortingPlaceholderControl>().Should().ContainSingle().Which.IsVisible.Should().BeFalse("no item is selected");
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void ColumnHeaders_AreTheLocalizedTranslations()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new ItemSearchControl(), harness.MainViewModel, 1300, 700);
        var grid = control.GetVisualDescendants().OfType<DataGrid>().Single();

        var headers = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Select(h => h.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()?.Text).Where(t => t != null).ToList();
        headers.Should().Equal(MainWindowTranslation.Icon, MainWindowTranslation.Name, MainWindowTranslation.Favorite);
    }

    [AvaloniaTest]
    public void Grid_ShowsTheItemsOfTheViewWithNameFavoriteAndAlertIcon()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new ItemSearchControl(), harness.MainViewModel, 1300, 700);
        var vm = harness.MainViewModel;
        var previous = vm.ItemsView;
        var favorite = new Item { UniqueName = "T4_BAG", IsFavorite = true, IsAlertActive = true };
        var plain = new Item { UniqueName = "T5_BAG" };
        vm.ItemsView = new System.Windows.Data.ListCollectionView(new List<Item> { favorite, plain });
        Dispatcher.UIThread.RunJobs();

        var grid = control.GetVisualDescendants().OfType<DataGrid>().Single();
        grid.GetVisualDescendants().OfType<DataGridRow>().Should().HaveCount(2);
        grid.GetVisualDescendants().OfType<CheckBox>().Where(c => c.TemplatedParent == null).Select(c => c.IsChecked).Should().BeEquivalentTo(new bool?[] { true, false });
        grid.GetVisualDescendants().OfType<FontAwesomeImage>().Select(i => i.IsVisible).Should().BeEquivalentTo(new[] { true, false });
        harness.Log.Errors.Should().BeEmpty();
        vm.ItemsView = previous;
    }

    [AvaloniaTest]
    public void InfoIcon_TogglesThePopupVisibilityOnTheViewModel()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new ItemSearchControl(), harness.MainViewModel, 1300, 700);
        var icon = control.GetVisualDescendants().OfType<Grid>().Single(g => g.Width == 20 && g.Height == 20);
        var inside = icon.TranslatePoint(new Point(10, 10), harness.Window)!.Value;

        harness.Window.MouseMove(inside);
        Dispatcher.UIThread.RunJobs();
        harness.MainViewModel.IsItemSearchPopupVisible.Should().Be(Visibility.Visible);
        harness.Window.MouseMove(new Point(2, 690));
        Dispatcher.UIThread.RunJobs();
        harness.MainViewModel.IsItemSearchPopupVisible.Should().Be(Visibility.Hidden);
    }

    [TestCase(double.NaN, 75, 75)]
    [TestCase(double.PositiveInfinity, 75, 75)]
    [TestCase(-1, 75, 75)]
    [TestCase(120, 75, 120)]
    [TestCase(0, 75, 0)]
    public void GetValidColumnWidth_KeepsOnlyFiniteNonNegativeWidths(double width, double fallback, double expected)
    {
        ItemSearchControl.GetValidColumnWidth(width, fallback).Should().Be(expected);
    }
}
