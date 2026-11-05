using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Alert;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.Models.NetworkModel;
using StatisticsAnalysisTool.UserControls;
using StatisticsAnalysisTool.ViewModels;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class ItemDetailsControlTests
{
    private static ItemDetailsViewModel CreateViewModel(ViewHarness harness)
    {
        var alertController = new AlertController(new System.Windows.Data.ListCollectionView(new List<Item>()));
        return new ItemDetailsViewModel(new Item { UniqueName = "T4_BAG" }, new ItemRefreshCooldownTracker(), alertController);
    }

    [AvaloniaTest]
    public void Renders_TheHeaderTheTabsAndThePriceGridsWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var viewModel = CreateViewModel(harness);
        var control = harness.Show(new ItemDetailsControl(), viewModel, 1100, 800);
        var tabs = control.GetVisualDescendants().OfType<TabControl>().First(t => t.TemplatedParent == null);

        for (var i = 0; i < tabs.Items.Count; i++)
        {
            tabs.SelectedIndex = i;
            Dispatcher.UIThread.RunJobs();
        }

        tabs.Items.Count.Should().BeGreaterThanOrEqualTo(4);
        control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().NotContain(t => t != null && t.Contains("Not yet ported"));
        harness.Log.Errors.Should().BeEmpty();
    }

    private static ItemPricesObject Price(string city, ulong sellPriceMin) => new(new MarketResponse
    {
        ItemTypeId = "T4_BAG",
        City = city,
        QualityLevel = 1,
        SellPriceMin = sellPriceMin,
        SellPriceMinDate = DateTime.UtcNow.AddMinutes(-5)
    }) { Visibility = System.Windows.Visibility.Visible };

    [AvaloniaTest]
    public void MainTabGrid_ShowsThePriceRowsAndAHeaderClickSortsThemByTheColumnTag()
    {
        using var harness = new ViewHarness();
        var viewModel = CreateViewModel(harness);
        var control = harness.Show(new ItemDetailsControl(), viewModel, 1400, 900);
        viewModel.MainTabBindings.ItemPrices = new System.Collections.ObjectModel.ObservableCollection<ItemPricesObject>
        {
            Price("Caerleon", 200),
            Price("Bridgewatch", 100),
            Price("Thetford", 300)
        };
        Dispatcher.UIThread.RunJobs();
        var grid = control.FindControl<DataGrid>("ItemPricesGrid")!;
        List<ulong> Prices() => grid.GetVisualDescendants().OfType<DataGridRow>().OrderBy(r => r.Bounds.Top).Select(r => ((ItemPricesObject) r.DataContext!).SellPriceMin).ToList();

        Prices().Should().Equal(200UL, 100UL, 300UL);

        var column = grid.Columns.Single(c => (string) c.Tag! == "SellPriceMin");
        var header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Single(h => h.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == ((TextBlock) column.Header).Text));
        var center = header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height / 2), harness.Window)!.Value;
        harness.Window.MouseDown(center, MouseButton.Left);
        harness.Window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Prices().Should().Equal(300UL, 200UL, 100UL);

        harness.Window.MouseDown(center, MouseButton.Left);
        harness.Window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Prices().Should().Equal(100UL, 200UL, 300UL);
        harness.Log.Errors.Should().BeEmpty();
    }
}
