using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Guild;
using StatisticsAnalysisTool.UserControls;
using System.Windows;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class GuildControlTests
{
    [AvaloniaTest]
    public void Renders_TheManualEntryCardTheTotalsAndBothListsWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new GuildControl(), harness.MainViewModel);

        control.GetVisualDescendants().OfType<TabItem>().Should().HaveCount(2);
        control.GetVisualDescendants().OfType<ComboBox>().Should().ContainSingle("the operator");
        control.GetVisualDescendants().OfType<ComboBoxItem>().Should().BeEmpty("the drop down is closed");
        control.GetVisualDescendants().OfType<TextBox>().Where(t => t.TemplatedParent == null).Should().HaveCount(2);
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void OperatorCombo_TwoWayBindsTheSelectedOperatorByTheTagOfTheItems()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new GuildControl(), harness.MainViewModel);
        var manual = harness.MainViewModel.GuildBindings.ManuallySiphonedEnergy;
        var combo = control.GetVisualDescendants().OfType<ComboBox>().Single();

        combo.SelectedIndex.Should().Be(manual.SelectedOperator == EnergyOperator.Deposit ? 0 : 1);
        combo.SelectedIndex = manual.SelectedOperator == EnergyOperator.Deposit ? 1 : 0;
        Dispatcher.UIThread.RunJobs();

        manual.SelectedOperator.Should().Be(manual.SelectedOperator == EnergyOperator.Deposit ? EnergyOperator.Deposit : EnergyOperator.Withdraw);
        ((EnergyOperator) ((ComboBoxItem) combo.SelectedItem!).Tag!).Should().Be(manual.SelectedOperator);
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void Items_ColorTheQuantityByDirectionAndDimDisabledEntries()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new GuildControl(), harness.MainViewModel);
        var bindings = harness.MainViewModel.GuildBindings;
        var withdraw = new SiphonedEnergyItem { CharacterName = "Alice", GuildName = "Guild", Timestamp = DateTime.UtcNow, Quantity = FixPoint.FromInternalValue(10_000) };
        var deposit = new SiphonedEnergyItem { CharacterName = "Bob", GuildName = "Guild", Timestamp = DateTime.UtcNow, Quantity = FixPoint.FromInternalValue(-10_000) };
        // The IsDisabled setter asks the (not running) tracking controller to refresh the overview; set the backing field instead.
        typeof(SiphonedEnergyItem).GetField("_isDisabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(deposit, true);
        bindings.SiphonedEnergyList.AddRange([withdraw, deposit]);
        bindings.SiphonedEnergyOverviewList.AddRange([withdraw, deposit]);
        bindings.SiphonedEnergyCollectionView.Refresh();
        Dispatcher.UIThread.RunJobs();

        var quantities = control.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains("deposit") || t.Classes.Contains("withdraw")).ToList();
        quantities.Count(t => t.Classes.Contains("deposit")).Should().Be(2, "one in the list and one in the overview");
        quantities.Count(t => t.Classes.Contains("withdraw")).Should().Be(2);
        quantities.Count(t => t.Classes.Contains("disabled")).Should().Be(1, "only the list row of the disabled entry");
        control.GetVisualDescendants().OfType<CheckBox>().Should().HaveCount(4);
        harness.Log.Errors.Should().BeEmpty();
        bindings.SiphonedEnergyList.Clear();
        bindings.SiphonedEnergyOverviewList.Clear();
    }

    [AvaloniaTest]
    public void SelectDeselectAll_TogglesEveryEntryForDeletion()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new GuildControl(), harness.MainViewModel);
        var bindings = harness.MainViewModel.GuildBindings;
        bindings.SiphonedEnergyList.AddRange([new SiphonedEnergyItem { CharacterName = "A", Timestamp = DateTime.UtcNow }, new SiphonedEnergyItem { CharacterName = "B", Timestamp = DateTime.UtcNow }]);
        var button = control.GetVisualDescendants().OfType<Button>().First(b => b.Content as string == bindings.TranslationSelectDeselectAll);

        button.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        bindings.SiphonedEnergyList.Should().OnlyContain(i => i.IsSelectedForDeletion);
        button.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        bindings.SiphonedEnergyList.Should().OnlyContain(i => !i.IsSelectedForDeletion);
        bindings.SiphonedEnergyList.Clear();
    }

    [AvaloniaTest]
    public void InfoIcon_ShowsTheDescriptionPopupWhileThePointerIsOverIt()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new GuildControl(), harness.MainViewModel, width: 1500);
        var popup = control.GetVisualDescendants().OfType<Grid>().Single(g => g.Background != null && g.Margin.Top == 220);
        popup.IsVisible.Should().BeFalse();

        var icon = control.GetVisualDescendants().OfType<Grid>().Single(g => g.Width == 20 && g.Height == 20);
        var inside = icon.TranslatePoint(new Point(10, 10), harness.Window)!.Value;
        harness.Window.MouseMove(inside);
        Dispatcher.UIThread.RunJobs();
        harness.MainViewModel.GuildBindings.GuildPopupVisibility.Should().Be(Visibility.Visible);
        popup.IsVisible.Should().BeTrue();

        harness.Window.MouseMove(new Point(2, 790));
        Dispatcher.UIThread.RunJobs();
        popup.IsVisible.Should().BeFalse();
        harness.Log.Errors.Should().BeEmpty();
    }
}
