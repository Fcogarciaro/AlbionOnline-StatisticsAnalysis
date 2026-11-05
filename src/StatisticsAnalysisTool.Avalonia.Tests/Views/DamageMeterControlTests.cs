using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.DamageMeter;
using StatisticsAnalysisTool.UserControls;
using StatisticsAnalysisTool.Views;
using System.Collections.ObjectModel;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class DamageMeterControlTests
{
    [AvaloniaTest]
    public void Renders_TheFilterBarListsAndInfoPanelWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new DamageMeterControl(), harness.MainViewModel, 1300, 800);

        control.GetVisualDescendants().OfType<Button>().Where(b => b.TemplatedParent == null).Should().HaveCountGreaterThanOrEqualTo(6);
        control.GetVisualDescendants().OfType<ComboBox>().Where(c => c.TemplatedParent == null).Should().HaveCount(2);
        control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Should().NotContain(t => t != null && t.Contains("Not yet ported"));
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void Fragments_AreShownByTheirStyleTypeWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var bindings = harness.MainViewModel.DamageMeterBindings;
        var previous = bindings.DamageMeter;
        bindings.DamageMeter = new ObservableCollection<DamageMeterFragment>
        {
            new() { Name = "Alpha", Damage = 1000, DamageInPercent = 50, DamagePercentage = 50, DamageMeterStyleFragmentType = DamageMeterStyleFragmentType.Damage },
            new() { Name = "Beta", Heal = 500, HealInPercent = 25, HealPercentage = 25, DamageMeterStyleFragmentType = DamageMeterStyleFragmentType.Heal },
            new() { Name = "Gamma", TakenDamage = 300, TakenDamageInPercent = 10, DamageMeterStyleFragmentType = DamageMeterStyleFragmentType.TakenDamage }
        };
        var control = harness.Show(new DamageMeterControl(), harness.MainViewModel, 1300, 800);
        Dispatcher.UIThread.RunJobs();

        var rows = control.GetVisualDescendants().OfType<Grid>().Where(g => g.MinHeight == 42).ToList();
        rows.Should().HaveCount(9, "three variants per fragment");
        rows.Count(r => r.IsEffectivelyVisible).Should().Be(3, "only the variant of the fragment type is shown");
        harness.Log.Errors.Should().BeEmpty();
        bindings.DamageMeter = previous;
    }

    [AvaloniaTest]
    public void Window_HostsTheListInsteadOfAPlaceholder()
    {
        using var harness = new ViewHarness();
        var bindings = harness.MainViewModel.DamageMeterBindings;
        var window = new DamageMeterWindow(bindings);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.GetVisualDescendants().OfType<ItemsControl>().Should().NotBeEmpty();
        window.GetVisualDescendants().OfType<ListBox>().Should().NotBeEmpty();
        harness.Log.Errors.Should().BeEmpty();
        window.Close();
    }
}
