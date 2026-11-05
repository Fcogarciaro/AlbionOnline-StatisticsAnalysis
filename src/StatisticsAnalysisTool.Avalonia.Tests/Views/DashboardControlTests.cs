using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
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
using StatisticsAnalysisTool.Views;
using System.Collections.ObjectModel;
using System.Windows;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class DashboardControlTests
{
    [AvaloniaTest]
    public void Renders_FiltersSummaryCardsAndTheHistoryChartWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        harness.UseTrackingController();
        var control = harness.Show(new DashboardControl(), harness.MainViewModel, width: 1300, height: 1000);

        var tabs = control.GetVisualDescendants().OfType<TabControl>().First();
        tabs.Items.Should().HaveCount(5, "dashboard, loot, combat, economy and mobs");
        control.GetVisualDescendants().OfType<CartesianChart>().Should().ContainSingle();
        control.GetVisualDescendants().OfType<ComboBox>().Where(c => c.TemplatedParent == null).Should().HaveCountGreaterThanOrEqualTo(4);
        control.GetVisualDescendants().OfType<Border>().Count(b => b.Cursor?.ToString() == "Hand" || b.Width == 32).Should().BeGreaterThan(0, "the open window button");
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void EveryTabRendersWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        harness.UseTrackingController();
        var control = harness.Show(new DashboardControl(), harness.MainViewModel, width: 1300, height: 1000);
        var tabs = control.GetVisualDescendants().OfType<TabControl>().First();

        for (var i = 0; i < tabs.Items.Count; i++)
        {
            tabs.SelectedIndex = i;
            Dispatcher.UIThread.RunJobs();
        }

        harness.Log.Errors.Should().BeEmpty();
        tabs.SelectedIndex = 0;
    }

    [AvaloniaTest]
    public void SectionHeader_TogglesTheSectionAndItsIconOnALeftPress()
    {
        using var harness = new ViewHarness();
        harness.UseTrackingController();
        var control = harness.Show(new DashboardControl(), harness.MainViewModel, width: 1300, height: 1000);
        var bindings = harness.MainViewModel.DashboardBindings;
        var before = bindings.FactionSummaryVisibility;
        var header = control.GetVisualDescendants().OfType<Grid>().First(g => g.Cursor != null && g.Height == 44);
        var point = header.TranslatePoint(new Point(40, 22), harness.Window)!.Value;

        harness.Window.MouseDown(point, MouseButton.Left);
        harness.Window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        bindings.FactionSummaryVisibility.Should().Be(before == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible);
        bindings.FactionSummaryToggleIcon.Should().Be(before == Visibility.Visible ? DSaladin.FontAwesome.WPF.FontAwesomeIcon.SolidPlus : DSaladin.FontAwesome.WPF.FontAwesomeIcon.SolidMinus);
        harness.Log.Errors.Should().BeEmpty();
        harness.Window.MouseDown(point, MouseButton.Left);
        harness.Window.MouseUp(point, MouseButton.Left);
    }

    [AvaloniaTest]
    public void HistoryChart_BindsTheViewModelSeriesAndAxes()
    {
        using var harness = new ViewHarness();
        harness.UseTrackingController();
        var control = harness.Show(new DashboardControl(), harness.MainViewModel, width: 1300, height: 2400);
        var chart = control.GetVisualDescendants().OfType<CartesianChart>().Single();

        chart.Series.Should().BeSameAs(harness.MainViewModel.SeriesDashboardHourValues);
        chart.XAxes.Should().BeSameAs(harness.MainViewModel.XAxesDashboardHourValues);
        // Pixel drawing of series is covered by GatheringControlTests (the dashboard controller refreshes these series itself).
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void LootControl_ShowsTheDetailSectionsOnlyWhenAskedTo()
    {
        using var harness = new ViewHarness();
        harness.UseTrackingController();
        var control = harness.Show(new DashboardLootControl(), harness.MainViewModel, width: 1100, height: 1200);
        var sections = control.GetVisualDescendants().OfType<Border>().Count(b => b.IsEffectivelyVisible && b.Classes.Count == 0 && b.Background != null && b.BorderThickness.Top == 1);
        harness.Log.Errors.Should().BeEmpty();
        control.ShowDetails.Should().BeTrue();

        control.ShowDetails = false;
        Dispatcher.UIThread.RunJobs();
        var reduced = control.GetVisualDescendants().OfType<Border>().Count(b => b.IsEffectivelyVisible && b.Classes.Count == 0 && b.Background != null && b.BorderThickness.Top == 1);

        reduced.Should().BeLessThan(sections, "the distribution and top areas sections are hidden");
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void DashboardWindow_HostsTheDashboardWithoutTheOpenWindowButton()
    {
        using var harness = new ViewHarness();
        harness.UseTrackingController();
        var window = new DashboardWindow(harness.MainViewModel);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var dashboard = window.GetVisualDescendants().OfType<DashboardControl>().Single();
        dashboard.ShowOpenWindowButton.Should().BeFalse();
        dashboard.DataContext.Should().BeSameAs(harness.MainViewModel);
        harness.Log.Errors.Should().BeEmpty();
        window.Close();
    }
}
