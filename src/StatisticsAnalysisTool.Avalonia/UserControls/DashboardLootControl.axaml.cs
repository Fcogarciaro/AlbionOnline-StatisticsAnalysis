using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using DSaladin.FontAwesome.WPF;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Windows;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>Port of UserControls/DashboardLootControl.xaml(.cs): the section headers toggle on a left press.</summary>
public partial class DashboardLootControl : UserControl
{
    public static readonly StyledProperty<bool> ShowDetailsProperty =
        AvaloniaProperty.Register<DashboardLootControl, bool>(nameof(ShowDetails), true);

    public bool ShowDetails
    {
        get => GetValue(ShowDetailsProperty);
        set => SetValue(ShowDetailsProperty, value);
    }

    public DashboardLootControl()
    {
        InitializeComponent();
    }

    private void LootStatsToggle_OnMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
    {
        if (!IsLeftPress(e) || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        ToggleSection(
            viewModel.DashboardBindings.LootStatsVisibility,
            value => viewModel.DashboardBindings.LootStatsVisibility = value,
            value => viewModel.DashboardBindings.LootStatsToggleIcon = value);
    }

    private void LootValueDistributionToggle_OnMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
    {
        if (!IsLeftPress(e) || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        ToggleSection(
            viewModel.DashboardBindings.LootValueDistributionVisibility,
            value => viewModel.DashboardBindings.LootValueDistributionVisibility = value,
            value => viewModel.DashboardBindings.LootValueDistributionToggleIcon = value);
    }

    private void LootTierEnchantmentToggle_OnMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
    {
        if (!IsLeftPress(e) || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        ToggleSection(
            viewModel.DashboardBindings.LootTierEnchantmentVisibility,
            value => viewModel.DashboardBindings.LootTierEnchantmentVisibility = value,
            value => viewModel.DashboardBindings.LootTierEnchantmentToggleIcon = value);
    }

    private void TopLootAreasToggle_OnMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
    {
        if (!IsLeftPress(e) || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        ToggleSection(
            viewModel.DashboardBindings.TopLootAreasVisibility,
            value => viewModel.DashboardBindings.TopLootAreasVisibility = value,
            value => viewModel.DashboardBindings.TopLootAreasToggleIcon = value);
    }

    private bool IsLeftPress(PointerPressedEventArgs e) => e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;

    private static void ToggleSection(
        Visibility currentVisibility,
        Action<Visibility> setVisibility,
        Action<FontAwesomeIcon> setIcon)
    {
        var isVisible = currentVisibility == Visibility.Visible;
        setVisibility(isVisible ? Visibility.Collapsed : Visibility.Visible);
        setIcon(isVisible ? FontAwesomeIcon.SolidPlus : FontAwesomeIcon.SolidMinus);
    }
}
