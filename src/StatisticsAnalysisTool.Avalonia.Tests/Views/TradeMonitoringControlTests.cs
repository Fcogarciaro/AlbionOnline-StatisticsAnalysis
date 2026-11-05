using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common.Controls;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.Trade;
using StatisticsAnalysisTool.UserControls;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class TradeMonitoringControlTests
{
    private static TradeMonitoringControl ShowWithHeatmapTab(ViewHarness harness)
    {
        var control = harness.Show(new TradeMonitoringControl(), harness.MainViewModel, 1400, 1100);
        var tabs = control.GetVisualDescendants().OfType<TabControl>().First(t => t.TemplatedParent == null);
        for (var i = 0; i < tabs.Items.Count; i++)
        {
            tabs.SelectedIndex = i;
            Dispatcher.UIThread.RunJobs();
            if (control.FindControl<SKElement>("ProfitByTimeOfDayHeatmap")?.GetVisualRoot() != null)
            {
                return control;
            }
        }

        throw new InvalidOperationException("no tab hosts the heatmap");
    }

    [AvaloniaTest]
    public void Renders_TheFilterCardTheTabsAndTheTradeListWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new TradeMonitoringControl(), harness.MainViewModel, 1400, 1100);

        control.GetVisualDescendants().OfType<TabItem>().Should().HaveCountGreaterThanOrEqualTo(5);
        control.GetVisualDescendants().OfType<CalendarDatePicker>().Should().HaveCount(2);
        control.GetVisualDescendants().OfType<ItemsControl>().Single(i => i.Name == "TradeList").ItemTemplate.Should().BeOfType<TradeDataTemplateSelector>();
        control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().NotContain(t => t != null && t.Contains("Not yet ported"));
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void EveryTabRendersWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new TradeMonitoringControl(), harness.MainViewModel, 1400, 1100);
        var tabs = control.GetVisualDescendants().OfType<TabControl>().First(t => t.TemplatedParent == null);

        for (var i = 0; i < tabs.Items.Count; i++)
        {
            tabs.SelectedIndex = i;
            Dispatcher.UIThread.RunJobs();
        }

        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void TradeList_UsesATemplatePerTradeType()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new TradeMonitoringControl(), harness.MainViewModel, 1400, 1100);
        var selector = control.GetVisualDescendants().OfType<ItemsControl>().Single(i => i.Name == "TradeList").ItemTemplate!;
        var trades = new[]
        {
            new Trade.Trade { Type = TradeType.Mail, MailTypeText = "MARKETPLACE_BUYORDER_FINISHED_SUMMARY" },
            new Trade.Trade { Type = TradeType.Mail, MailTypeText = "MARKETPLACE_SELLORDER_FINISHED_SUMMARY" },
            new Trade.Trade { Type = TradeType.Mail, MailTypeText = "MARKETPLACE_SELLORDER_EXPIRED_SUMMARY" },
            new Trade.Trade { Type = TradeType.Mail, MailTypeText = "MARKETPLACE_BUYORDER_EXPIRED_SUMMARY" },
            new Trade.Trade { Type = TradeType.InstantSell },
            new Trade.Trade { Type = TradeType.InstantBuy },
            new Trade.Trade { Type = TradeType.ManualSell },
            new Trade.Trade { Type = TradeType.ManualBuy },
            new Trade.Trade { Type = TradeType.Crafting },
            new Trade.Trade { Type = TradeType.PlayerTradeIncoming }
        };
        var panel = new StackPanel();
        foreach (var trade in trades)
        {
            panel.Children.Add(new ContentControl { Content = trade, ContentTemplate = selector });
        }

        harness.Window.Content = panel;
        Dispatcher.UIThread.RunJobs();

        panel.GetVisualDescendants().OfType<global::Avalonia.Controls.Presenters.ContentPresenter>().Count(p => p.Content is Trade.Trade && p.Child != null).Should().Be(trades.Length);
        harness.Log.Errors.Should().BeEmpty();
    }

    private static TradeProfitTimeOfDayPoint Point(DayOfWeek day, int hour, double net) => new() { DayOfWeek = day, Hour = hour, NetProfit = net, TradeCount = 1 };

    private static global::Avalonia.Media.Color PixelAt(Window window, Point point)
    {
        window.CaptureRenderedFrame();
        using var frame = window.CaptureRenderedFrame()!;
        using var framebuffer = frame.Lock();
        var bytes = new byte[4];
        System.Runtime.InteropServices.Marshal.Copy(framebuffer.Address + (int) point.Y * framebuffer.RowBytes + (int) point.X * 4, bytes, 0, 4);
        return framebuffer.Format == global::Avalonia.Platform.PixelFormat.Bgra8888
            ? global::Avalonia.Media.Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0])
            : global::Avalonia.Media.Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
    }

    private static Point CellCenter(SKElement heatmap, Window window, int day, int hour)
    {
        // The layout constants of the upstream drawing code: paddings 72/24/14/38, gap 4, 24 hours by 7 days.
        var cellWidth = (heatmap.Bounds.Width - 72 - 14 - 23 * 4) / 24;
        var cellHeight = (heatmap.Bounds.Height - 24 - 38 - 6 * 4) / 7;
        return heatmap.TranslatePoint(new Point(72 + hour * (cellWidth + 4) + cellWidth / 2, 24 + day * (cellHeight + 4) + cellHeight / 2), window)!.Value;
    }

    [AvaloniaTest]
    public void Heatmap_DrawsGreenForProfitAndRedForLossCellsAndNeutralForZero()
    {
        using var harness = new ViewHarness();
        var control = ShowWithHeatmapTab(harness);
        var bindings = harness.MainViewModel.TradeMonitoringBindings;
        var all = new List<TradeProfitTimeOfDayPoint>();
        for (var d = 0; d < 7; d++)
        {
            for (var h = 0; h < 24; h++)
            {
                all.Add(Point((DayOfWeek) ((d + 1) % 7), h, 0));
            }
        }

        all[3] = Point(DayOfWeek.Monday, 3, 1000);
        all[24 + 5] = Point(DayOfWeek.Tuesday, 5, -400);
        bindings.ProfitByTimeOfDayHeatmapVisibility = System.Windows.Visibility.Visible;
        bindings.ProfitByTimeOfDayHeatmapPoints = all;
        Dispatcher.UIThread.RunJobs();
        var heatmap = control.FindControl<SKElement>("ProfitByTimeOfDayHeatmap")!;
        heatmap.IsEffectivelyVisible.Should().BeTrue();

        var profit = PixelAt(harness.Window, CellCenter(heatmap, harness.Window, 0, 3));
        var loss = PixelAt(harness.Window, CellCenter(heatmap, harness.Window, 1, 5));
        var zero = PixelAt(harness.Window, CellCenter(heatmap, harness.Window, 0, 0));
        (profit.G > profit.R + 40 && profit.G > profit.B + 20).Should().BeTrue($"the profit cell is green but is {profit}");
        (loss.R > loss.G + 60 && loss.R > loss.B + 40).Should().BeTrue($"the loss cell is red but is {loss}");
        (Math.Abs(zero.R - zero.G) < 30 && Math.Abs(zero.G - zero.B) < 30).Should().BeTrue($"a zero cell is neutral but is {zero}");
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void Heatmap_ShowsTheTooltipOfTheCellUnderThePointer()
    {
        using var harness = new ViewHarness();
        var control = ShowWithHeatmapTab(harness);
        var bindings = harness.MainViewModel.TradeMonitoringBindings;
        bindings.ProfitByTimeOfDayHeatmapVisibility = System.Windows.Visibility.Visible;
        var points = Enumerable.Range(0, 7).SelectMany(d => Enumerable.Range(0, 24).Select(h => Point((DayOfWeek) ((d + 1) % 7), h, d == 0 && h == 0 ? 500 : 0))).ToList();
        bindings.ProfitByTimeOfDayHeatmapPoints = points;
        Dispatcher.UIThread.RunJobs();
        var heatmap = control.FindControl<SKElement>("ProfitByTimeOfDayHeatmap")!;

        var cellWidth = (heatmap.Bounds.Width - 72 - 14 - 23 * 4) / 24;
        var cellHeight = (heatmap.Bounds.Height - 24 - 38 - 6 * 4) / 7;
        var inCell = heatmap.TranslatePoint(new Point(72 + cellWidth / 2, 24 + cellHeight / 2), harness.Window)!.Value;
        harness.Window.MouseMove(inCell);
        Dispatcher.UIThread.RunJobs();
        ToolTip.GetTip(heatmap).Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeEmpty();

        var outside = heatmap.TranslatePoint(new Point(10, 10), harness.Window)!.Value;
        harness.Window.MouseMove(outside);
        Dispatcher.UIThread.RunJobs();
        ToolTip.GetTip(heatmap).Should().BeNull("the pointer left the cells");
    }
}
