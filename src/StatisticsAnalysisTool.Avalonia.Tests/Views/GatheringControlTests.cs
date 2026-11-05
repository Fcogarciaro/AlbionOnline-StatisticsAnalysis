using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Painting;
using NUnit.Framework;
using SkiaSharp;
using StatisticsAnalysisTool.Avalonia.Tests.Icons;
using StatisticsAnalysisTool.UserControls;
using System.Collections.ObjectModel;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class GatheringControlTests
{
    [AvaloniaTest]
    public void Renders_FilterCardSevenTabsAndTheChartsWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new GatheringControl(), harness.MainViewModel, width: 1500, height: 1000);

        control.GetVisualDescendants().OfType<TabItem>().Should().HaveCount(7, "generally plus wood, fiber, ore, hide, rock and fishing");
        control.GetVisualDescendants().OfType<PieChart>().Should().HaveCount(2);
        control.GetVisualDescendants().OfType<CartesianChart>().Should().ContainSingle();
        control.GetVisualDescendants().OfType<ComboBox>().Should().HaveCount(3, "time range, session and chart value type");
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void PieChart_DrawsTheBoundSeries()
    {
        using var harness = new ViewHarness();
        var stats = harness.MainViewModel.GatheringBindings.GatheringStats;
        stats.ResourceValueByTypeSeries =
        [
            new PieSeries<double> { Name = "Ore", Values = [70d], InnerRadius = 45, Stroke = null, Fill = new SolidColorPaint(new SKColor(255, 0, 255)) },
            new PieSeries<double> { Name = "Wood", Values = [30d], InnerRadius = 45, Stroke = null, Fill = new SolidColorPaint(new SKColor(0, 255, 0)) }
        ];
        var control = harness.Show(new GatheringControl(), harness.MainViewModel, width: 1500, height: 1000);

        var pie = control.GetVisualDescendants().OfType<PieChart>().First();
        pie.Series.Should().BeSameAs(stats.ResourceValueByTypeSeries);
        // The chart paints on the render timer; give it a few frames.
        var magenta = 0;
        for (var i = 0; i < 30 && magenta == 0; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(5);
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(60);
            magenta = FontAwesomeImageTests.CountPixelsIn(harness.Window, c => c.R > 200 && c.G < 60 && c.B > 200);
        }

        magenta.Should().BeGreaterThan(200, "the pie slice is drawn by SkiaSharp");
        harness.Log.Errors.Should().BeEmpty();
        stats.ResourceValueByTypeSeries = [];
    }

    [AvaloniaTest]
    public void CartesianChart_DrawsTheBoundLine()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new GatheringControl(), harness.MainViewModel, width: 1500, height: 1000);
        var stats = harness.MainViewModel.GatheringBindings.GatheringStats;
        stats.ResourceChartSeries = [new LineSeries<double> { Values = [1d, 3d, 2d], Stroke = new SolidColorPaint(new SKColor(255, 0, 255), 6), Fill = null, GeometrySize = 0 }];
        stats.ResourceChartXAxes = [new Axis { Labels = ["a", "b", "c"] }];
        var n = 0;
        for (var i = 0; i < 20 && n == 0; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(5);
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(60);
            n = FontAwesomeImageTests.CountPixelsIn(harness.Window, c => c.R > 200 && c.G < 60 && c.B > 200);
        }
        n.Should().BeGreaterThan(100, "the line is drawn by SkiaSharp");
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void CartesianChart_BindsSeriesAndAxes()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new GatheringControl(), harness.MainViewModel, width: 1500, height: 1000);
        var stats = harness.MainViewModel.GatheringBindings.GatheringStats;
        stats.ResourceChartSeries = [new LineSeries<double> { Values = [1d, 3d, 2d] }];
        stats.ResourceChartXAxes = [new Axis { Labels = ["a", "b", "c"] }];
        Dispatcher.UIThread.RunJobs();

        var chart = control.GetVisualDescendants().OfType<CartesianChart>().Single();
        chart.Series.Should().BeSameAs(stats.ResourceChartSeries);
        chart.XAxes.Should().BeSameAs(stats.ResourceChartXAxes);
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void ActivationToggle_FollowsTheViewModel()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new GatheringControl(), harness.MainViewModel, width: 1500, height: 1000);
        var bindings = harness.MainViewModel.GatheringBindings;
        var icon = control.GetVisualDescendants().OfType<StatisticsAnalysisTool.Common.Controls.FontAwesomeImage>().First();
        var before = bindings.IsGatheringActive;

        icon.Classes.Contains("on").Should().Be(before);
        bindings.IsGatheringActive = !before;
        Dispatcher.UIThread.RunJobs();

        icon.Classes.Contains("on").Should().Be(!before);
        bindings.IsGatheringActive = before;
    }

    [AvaloniaTest]
    public void ViewReportsItsVisibilityToTheViewModel()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new GatheringControl(), harness.MainViewModel, width: 1500, height: 1000);
        var bindings = harness.MainViewModel.GatheringBindings;
        var field = typeof(StatisticsAnalysisTool.Models.BindingModel.GatheringBindings).GetField("_isGatheringViewVisible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        ((bool) field.GetValue(bindings)!).Should().BeTrue();
        control.IsVisible = false;
        Dispatcher.UIThread.RunJobs();
        ((bool) field.GetValue(bindings)!).Should().BeFalse();
    }

    [AvaloniaTest]
    public void ResourceTab_ListsTheGatheredEntriesInAVirtualizedList()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new GatheringControl(), harness.MainViewModel, width: 1500, height: 1000);
        var bindings = harness.MainViewModel.GatheringBindings;
        var stats = bindings.GatheringStats;
        stats.GatheredWood.AddRange(Enumerable.Range(0, 3000).Select(i => new StatisticsAnalysisTool.Gathering.Gathered
        {
            TimestampUtc = DateTime.UtcNow.Ticks,
            GainedTotalAmount = i + 1
        }));
        var tab = control.GetVisualDescendants().OfType<TabControl>().First();
        tab.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();

        var rows = control.GetVisualDescendants().OfType<ContentPresenter>().Count(p => p.Content is StatisticsAnalysisTool.Gathering.Gathered);
        rows.Should().BeGreaterThan(0).And.BeLessThan(150, "3000 entries must not all be realized");
        harness.Log.Errors.Should().BeEmpty();
        stats.GatheredWood.Clear();
        tab.SelectedIndex = 0;
    }
}
