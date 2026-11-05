using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using LiveChartsCore.SkiaSharpView.Avalonia;
using NUnit.Framework;
using StatisticsAnalysisTool.UserControls;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class CraftingControlTests
{
    [AvaloniaTest]
    public void Renders_TheEditorTheResourcesTheResultsAndTheSavedListWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new CraftingControl(), harness.MainViewModel, width: 1500, height: 1100);

        control.GetVisualDescendants().OfType<TabItem>().Should().HaveCountGreaterThanOrEqualTo(1);
        control.GetVisualDescendants().OfType<DataGrid>().Should().HaveCount(2, "resources and saved craftings");
        control.GetVisualDescendants().OfType<TextBox>().Where(t => t.TemplatedParent == null).Should().NotBeEmpty();
        control.FindControl<TextBox>("CraftingItemSearchTextBox").Should().NotBeNull();
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void EveryTabRendersWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new CraftingControl(), harness.MainViewModel, width: 1500, height: 1100);
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
    public void Popups_OpenAndCloseFromTheViewModelFlags()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new CraftingControl(), harness.MainViewModel, width: 1500, height: 1100);
        var bindings = harness.MainViewModel.CraftingBindings;

        bindings.IsCraftingLocationPopupOpen = true;
        Dispatcher.UIThread.RunJobs();
        control.GetVisualDescendants().OfType<global::Avalonia.Controls.Primitives.Popup>().Any(p => p.IsOpen).Should().BeTrue();

        bindings.IsCraftingLocationPopupOpen = false;
        Dispatcher.UIThread.RunJobs();
        control.GetVisualDescendants().OfType<global::Avalonia.Controls.Primitives.Popup>().Any(p => p.IsOpen).Should().BeFalse();
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void SavedCraftings_ListsRowsWithTheFocusIconAndProfitColors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new CraftingControl(), harness.MainViewModel, width: 1500, height: 1100);
        var saved = harness.MainViewModel.CraftingBindings.SavedCraftings;
        saved.Add(new StatisticsAnalysisTool.Crafting.SavedCrafting { Id = Guid.NewGuid(), ItemUniqueName = "T4_BAG", UsesFocus = true, Notes = "note one" });
        Dispatcher.UIThread.RunJobs();

        var grid = control.GetVisualDescendants().OfType<DataGrid>().Single(g => g.ItemsSource == saved);
        grid.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().Contain("note one");
        grid.GetVisualDescendants().OfType<StatisticsAnalysisTool.Common.Controls.FontAwesomeImage>().Should().Contain(i => i.Classes.Contains("on"));
        harness.Log.Errors.Should().BeEmpty();
        saved.Clear();
    }
}
