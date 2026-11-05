using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;

namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

[NonParallelizable]
public class SecondaryWindowsTests
{
    private BindingErrorLog _log;

    [SetUp]
    public void SetUp() => _log = new BindingErrorLog();

    [TearDown]
    public void TearDown() => _log.Dispose();

    private static void Click(Control root, string name) =>
        root.GetVisualDescendants().OfType<Button>().Single(b => b.Name == name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static IEnumerable<string> Texts(Control root) =>
        root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Where(t => !string.IsNullOrEmpty(t));

    // ---- DialogWindow

    [AvaloniaTest]
    public void DialogWindow_YesNo_ShowsBothButtonsAndYesConfirms()
    {
        var window = new DialogWindow("Reset", "Are you sure?");
        window.Show();

        var buttons = window.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyVisible && b.Name != "CloseButton").ToList();
        buttons.Should().HaveCount(2, "yes and no");
        Texts(window).Should().Contain("Are you sure?");
        window.DialogWindowViewModel.Type.Should().Be(DialogType.YesNo);

        window.GetVisualDescendants().OfType<Button>().First(b => b.IsEffectivelyVisible && b.Name != "CloseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        window.DialogWindowViewModel.Canceled.Should().BeFalse();
        window.DialogResult.Should().BeTrue();
        _log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void DialogWindow_CloseButtonCancels()
    {
        var window = new DialogWindow("Reset", "Are you sure?");
        window.Show();

        Click(window, "CloseButton");

        window.DialogWindowViewModel.Canceled.Should().BeTrue();
        window.DialogResult.Should().BeFalse();
    }

    [AvaloniaTest]
    public void DialogWindow_InfoWithUrl_ShowsTheLinkAndOnlyTheOkButton()
    {
        var window = new DialogWindow("Info", "Message body", DialogType.Ok, "https://man7.org/linux/man-pages/man7/capabilities.7.html", "About capabilities", 300d);
        window.Show();

        var link = window.FindControl<TextBlock>("UrlTextBlock")!;
        link.IsEffectivelyVisible.Should().BeTrue();
        link.Text.Should().Be("About capabilities");
        window.MaxHeight.Should().Be(300);
        window.GetVisualDescendants().OfType<Button>().Count(b => b.IsEffectivelyVisible).Should().Be(2, "close and OK");
        _log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void DialogWindow_WithoutUrl_HidesTheLink()
    {
        var window = new DialogWindow("Info", "Message body", DialogType.Ok);
        window.Show();

        window.FindControl<TextBlock>("UrlTextBlock")!.IsEffectivelyVisible.Should().BeFalse();
    }

    // ---- InfoWindow

    [AvaloniaTest]
    public void InfoWindow_ShowsTheLinuxCaptureLinkInsteadOfNpcap()
    {
        var window = new InfoWindow();
        window.Show();

        var links = window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Tag is string).Select(t => (string) t.Tag).ToList();
        links.Should().BeEquivalentTo("https://man7.org/linux/man-pages/man7/capabilities.7.html", "https://www.albion-online-data.com/");
        Texts(window).Should().NotContain(t => t.Contains("npcap", StringComparison.OrdinalIgnoreCase));
        window.GetVisualDescendants().OfType<CheckBox>().Should().ContainSingle();
        _log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void InfoWindow_CloseAndMinimizeButtonsWork()
    {
        var window = new InfoWindow();
        window.Show();

        Click(window, "MinimizeButton");
        window.WindowState.Should().Be(global::Avalonia.Controls.WindowState.Minimized);
        Click(window, "CloseButton");
        window.IsVisible.Should().BeFalse();
    }

    // ---- ItemAlertWindow

    [AvaloniaTest]
    public void ItemAlertWindow_CityBackground_IsATransparentCityTransparentGradient()
    {
        var brush = (LinearGradientBrush) ItemAlertWindow.CreateCityBackground(Color.Parse("#FF112233"));

        brush.GradientStops.Select(s => s.Offset).Should().Equal(0, 0.5, 1);
        brush.GradientStops[1].Color.Should().Be(Color.Parse("#FF112233"));
        brush.GradientStops[0].Color.A.Should().Be(0);
        brush.GradientStops[2].Color.A.Should().Be(0);
    }

    // ---- LootComparatorSaveNameWindow

    [AvaloniaTest]
    public void LootComparatorSaveName_SaveIsEnabledOnlyWithATrimmedName()
    {
        var window = new LootComparatorSaveNameWindow();
        window.Show();
        var save = window.FindControl<Button>("SaveButton")!;
        var box = window.FindControl<TextBox>("SaveNameTextBox")!;
        save.IsEnabled.Should().BeFalse();

        box.Text = "   ";
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        save.IsEnabled.Should().BeFalse();
        box.Text = "  my loot  ";
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        save.IsEnabled.Should().BeTrue();
        save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        window.SaveName.Should().Be("my loot");
        window.DialogResult.Should().BeTrue();
        _log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void LootComparatorSaveName_CancelReturnsFalseAndNoName()
    {
        var window = new LootComparatorSaveNameWindow();
        window.Show();

        Click(window, "CancelButton");

        window.DialogResult.Should().BeFalse();
        window.SaveName.Should().BeEmpty();
    }

    // ---- LanguageSelectionWindow

    [AvaloniaTest]
    public void LanguageSelection_ListsLanguagesAndConfirmClosesWithTrue()
    {
        StatisticsAnalysisTool.Localization.LocalizationController.Init().Should().BeTrue();
        var window = new LanguageSelectionWindow();
        window.Show();
        var viewModel = (LanguageSelectionWindowViewModel) window.DataContext!;
        var combo = window.FindControl<ComboBox>("LanguageComboBox")!;

        viewModel.Languages.Should().NotBeEmpty("the localization table lists its languages");
        combo.ItemCount.Should().Be(viewModel.Languages.Count);
        combo.SelectedItem.Should().BeSameAs(viewModel.SelectedFileInformation);

        window.GetVisualDescendants().OfType<Button>().Single(b => (b.Content as string) == "Ok").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        window.DialogResult.Should().BeTrue();
        _log.Errors.Should().BeEmpty();
    }

    // ---- GameDataPreparationWindow

    private static GameDataPreparationWindow GameWindow(Func<string, bool> valid, params string[] detected) =>
        new(valid, () => detected, () => "/start");

    [AvaloniaTest]
    public void GameDataPreparation_PreselectsTheFirstDetectedFolderAndConfirmReturnsIt()
    {
        var window = GameWindow(_ => true, "/home/u/.local/share/Steam/steamapps/common/Albion Online", "/home/u/.wine/drive_c/Program Files (x86)/AlbionOnline");
        window.Show();

        window.FindControl<StackPanel>("DetectedPanel")!.IsVisible.Should().BeTrue();
        window.FindControl<ItemsControl>("DetectedFolders")!.ItemCount.Should().Be(2);
        window.SelectedFolder.Should().Be("/home/u/.local/share/Steam/steamapps/common/Albion Online");
        var confirm = window.FindControl<Button>("ConfirmButton")!;
        confirm.IsEnabled.Should().BeTrue();

        confirm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        window.DialogResult.Should().BeTrue();
        _log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void GameDataPreparation_ClickingAnotherDetectedFolderSelectsIt()
    {
        var window = GameWindow(_ => true, "/a/Albion Online", "/b/AlbionOnline");
        window.Show();

        var second = window.FindControl<ItemsControl>("DetectedFolders")!.GetVisualDescendants().OfType<Button>().Single(b => (string) b.Tag == "/b/AlbionOnline");
        second.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        window.SelectedFolder.Should().Be("/b/AlbionOnline");
    }

    [AvaloniaTest]
    public void GameDataPreparation_WithoutDetectionTheConfirmButtonIsDisabledAndTheListHidden()
    {
        var window = GameWindow(_ => true);
        window.Show();

        window.FindControl<StackPanel>("DetectedPanel")!.IsVisible.Should().BeFalse();
        window.FindControl<Button>("ConfirmButton")!.IsEnabled.Should().BeFalse();
        window.SelectedFolder.Should().BeNullOrEmpty();
    }

    [AvaloniaTest]
    public void GameDataPreparation_AnInvalidDetectedFolderIsNotConfirmable()
    {
        var window = GameWindow(_ => false, "/not/the/game");
        window.Show();

        window.FindControl<Button>("ConfirmButton")!.IsEnabled.Should().BeFalse();
        ((GameDataPreparationWindowViewModel) window.DataContext!).ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ---- EventValidationWindow

    [AvaloniaTest]
    public void EventValidationWindow_ShowsTheEventListWithTheResetButton()
    {
        var window = new EventValidationWindow();
        window.Show();

        var list = window.FindControl<ListBox>("EventList")!;
        StatisticsAnalysisTool.EventValidations.EventValidator.ValidatingEvents.Add(new StatisticsAnalysisTool.EventValidations.EventValidityItem
        {
            EventCode = StatisticsAnalysisTool.Network.EventCodes.NewCharacter,
            Status = StatisticsAnalysisTool.EventValidations.EventValidityStatus.Valid
        });
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        list.ItemCount.Should().Be(StatisticsAnalysisTool.EventValidations.EventValidator.ValidatingEvents.Count);
        var icon = list.GetVisualDescendants().OfType<StatisticsAnalysisTool.Common.Controls.FontAwesomeImage>().First(i => i.Classes.Contains("valid"));
        icon.Icon.Should().Be(DSaladin.FontAwesome.WPF.FontAwesomeIcon.SolidCheck, "the valid status draws a check mark");
        window.GetVisualDescendants().OfType<Button>().Count(b => b.Content is string { Length: > 0 }).Should().BeGreaterThan(0);
        _log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void ResizableWindows_ToggleMaximizeFromTheirButtonAndHideTheResizeGrip()
    {
        var window = new EventValidationWindow();
        window.Show();
        var handles = window.GetVisualDescendants().OfType<StatisticsAnalysisTool.Common.Controls.WindowResizeHandles>().Single();
        handles.IsVisible.Should().BeTrue();

        Click(window, "MaximizedButton");

        window.WindowState.Should().Be(global::Avalonia.Controls.WindowState.Maximized);
        handles.IsVisible.Should().BeFalse();
        Click(window, "MaximizedButton");
        handles.IsVisible.Should().BeTrue();
    }

    // ---- DashboardWindow / DamageMeterWindow

    [AvaloniaTest]
    public void DashboardWindow_HostsThePlaceholderWithTheLocalizedTitle()
    {
        var window = new DashboardWindow(new MainWindowViewModel());
        window.Show();

        window.Title.Should().NotBeNullOrEmpty();
        window.GetVisualDescendants().OfType<StatisticsAnalysisTool.UserControls.PortingPlaceholderControl>().Should().ContainSingle();
        _log.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void DamageMeterWindow_RendersWithItsViewModel()
    {
        var window = new DamageMeterWindow(new StatisticsAnalysisTool.DamageMeter.DamageMeterBindings());
        window.Show();

        window.DataContext.Should().BeOfType<DamageMeterWindowViewModel>();
        window.Title.Should().NotBeNullOrEmpty();
        _log.Errors.Should().BeEmpty();
    }
}
