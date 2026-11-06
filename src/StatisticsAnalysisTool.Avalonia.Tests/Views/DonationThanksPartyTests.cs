using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common.Controls;
using StatisticsAnalysisTool.Common.Converters;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.UserControls;
using StatisticsAnalysisTool.ViewModels;
using System.Text.RegularExpressions;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class DonationThanksPartyTests
{
    [AvaloniaTest]
    public void ThanksControl_ShowsTheLocalizedTitleAndEveryThanksLine()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new ThanksControl());

        control.DataContext.Should().BeOfType<ThanksViewModel>();
        var texts = control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        texts.Should().Contain(StatisticsAnalysisTool.Models.TranslationModel.ThanksTranslation.ThankYou);
        texts.Should().Contain(["Rhaegaryn", "BloodySupport", "Nuberu", "ZwergGimli", "arabinos", "Ewerson (Maligors)", "Me1onSeed", "SeoheeKhang", "Draxhut"]);
        control.GetVisualDescendants().OfType<Image>().Count(i => i.Source != null).Should().Be(3, "three hearts");
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void DonationControl_RendersGoalsListsAndLinkIconsWithoutBindingErrors()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new DonationControl());

        control.DataContext.Should().BeOfType<DonationViewModel>();
        control.GetVisualDescendants().OfType<ProgressBar>().Should().HaveCount(2);
        control.GetVisualDescendants().OfType<ItemsControl>().Should().HaveCount(3);
        var icons = control.GetVisualDescendants().OfType<FontAwesomeImage>().Where(i => i.FindAncestorOfType<Grid>()!.Classes.Contains("link")).ToList();
        icons.Should().HaveCount(3, "Patreon, PayPal and GitHub Sponsors");
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void DonationControl_LinkAreasCarryTheUrlsOfTheViewModelAndOnlyOpenWebLinks()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new DonationControl());

        var links = control.GetVisualDescendants().OfType<Control>().Where(c => c.Classes.Contains("link")).ToList();
        links.Select(l => l.Tag).Should().BeEquivalentTo([DonationViewModel.PatreonUrl, DonationViewModel.DonateUrl, DonationViewModel.KofiDonationUrl, DonationViewModel.GitHubSponsorsUrl]);
        links.Should().OnlyContain(l => l.Cursor != null && l.Cursor.ToString() == new Cursor(StandardCursorType.Hand).ToString());
    }

    [AvaloniaTest]
    public void PartyControl_HasTheDeathAlertAndPartyBuilderTabsOnTheMainViewModel()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new PartyControl(), harness.MainViewModel);

        var tabs = control.GetVisualDescendants().OfType<TabItem>().ToList();
        tabs.Should().HaveCount(2);
        tabs.Select(t => t.Header).Should().BeEquivalentTo([StatisticsAnalysisTool.Party.PartyBindings.TranslationDeathAlert, StatisticsAnalysisTool.Party.PartyBindings.TranslationPartyBuilder]);
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void PartyControl_PartyBuilderTabShowsConditionsInformationAndDescriptions()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new PartyControl(), harness.MainViewModel);
        var tab = control.GetVisualDescendants().OfType<TabControl>().Single();
        tab.SelectedIndex = 1;
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        var texts = control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        texts.Should().Contain([StatisticsAnalysisTool.Party.PartyBindings.TranslationConditions, StatisticsAnalysisTool.Party.PartyBindings.TranslationPartyInformation, StatisticsAnalysisTool.Party.PartyBindings.TranslationDescriptions]);
        control.GetVisualDescendants().OfType<TextBox>().Should().HaveCount(4);
        control.GetVisualDescendants().OfType<GridSplitter>().Should().ContainSingle();
        harness.Log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void PartyControl_ListsPartyPlayersInBothTabs()
    {
        using var harness = new ViewHarness();
        var control = harness.Show(new PartyControl(), harness.MainViewModel);
        var party = harness.MainViewModel.PartyBindings;
        party.Party.Add(new StatisticsAnalysisTool.Party.PartyPlayer { Guid = Guid.NewGuid(), Username = "Alice", IsLocalPlayer = true });
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        control.GetVisualDescendants().OfType<Label>().Select(l => l.Content).Should().Contain("Alice");
        control.GetVisualDescendants().OfType<CheckBox>().Should().ContainSingle();

        control.GetVisualDescendants().OfType<TabControl>().Single().SelectedIndex = 1;
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        control.GetVisualDescendants().OfType<Label>().Select(l => l.Content).Should().Contain("Alice");
        harness.Log.Errors.Should().BeEmpty();
        party.Party.Clear();
    }
}
