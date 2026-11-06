using StatisticsAnalysisTool.Crafting;
using StatisticsAnalysisTool.GameFileData;
using StatisticsAnalysisTool.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Windows;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>
/// Port of UserControls/CraftingControl.xaml(.cs). WPF preview mouse events are tunnelling pointer handlers on the control; the keyboard focus
/// loss handlers look at the focused element after the focus moved (Avalonia has no NewFocus), popup lists are separate top levels.
/// </summary>
public partial class CraftingControl : UserControl
{
    private bool _wasPriceTextBoxFocusedOnMouseDown;

    public CraftingControl()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel, true);
        AddHandler(PointerReleasedEvent, OnPreviewPointerReleased, RoutingStrategies.Tunnel, true);
    }

    private void OnPreviewPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var source = e.Source as Visual;
        if (source?.FindAncestorOfType<TextBox>(true) is { } box && box.Classes.Contains("price-input"))
        {
            PriceTextBox_OnPreviewMouseLeftButtonDown(box, e);
        }

        CraftingControl_OnPreviewMouseDown(sender, e);
    }

    private void OnPreviewPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left || (e.Source as Visual)?.FindAncestorOfType<TextBox>(true) is not { } box)
        {
            return;
        }

        if (ReferenceEquals(box, CraftingItemSearchTextBox))
        {
            CraftingItemSearch_OnPreviewMouseLeftButtonUp(box, e);
        }
        else if (ReferenceEquals(box, OutputUnitPriceTextBox))
        {
            OutputUnitPrice_OnPreviewMouseLeftButtonUp(box, e);
        }
        else if (ReferenceEquals(box, EmptyJournalPriceTextBox))
        {
            JournalEmptyPrice_OnPreviewMouseLeftButtonUp(box, e);
        }
        else if (ReferenceEquals(box, FullJournalPriceTextBox))
        {
            JournalFullPrice_OnPreviewMouseLeftButtonUp(box, e);
        }
        else if (box.Classes.Contains("resource-price"))
        {
            ResourcePrice_OnPreviewMouseLeftButtonUp(box, e);
        }
    }

    private void SavedCraftings_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not DataGrid listView)
        {
            return;
        }

        if (listView.SelectedItem is not SavedCrafting savedCrafting)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.LoadSelectedCommand.Execute(savedCrafting);
    }

    private void ListBoxItemSearch_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingItemSearchResult searchResult)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.SelectItemSearchResult(searchResult);
        listBox.SelectedItem = null;
    }

    private void CraftingItemSearch_OnGotKeyboardFocus(object sender, RoutedEventArgs e)
    {
        OpenItemSearch();
    }

    private void CraftingItemSearch_OnPreviewMouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
    {
        OpenItemSearch();
    }

    private void OpenItemSearch()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenItemSearch();
    }

    private void CraftingItemSearch_OnLostKeyboardFocus(object sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var newFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;

            if (IsElementOrChildOf(newFocus, CraftingItemSearchTextBox)
                || IsElementOrChildOf(newFocus, CraftingItemSearchListBox))
            {
                return;
            }

            CloseItemSearch();
        });
    }

    private void ListBoxCraftingLocation_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingLocationOption location)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.SelectCraftingLocation(location);
        listBox.SelectedItem = null;
    }

    private void ListBoxSellPrice_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingSellPriceOption sellPriceOption)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.SelectSellPriceOption(sellPriceOption);
        listBox.SelectedItem = null;
    }

    private void ListBoxResourcePrice_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.Tag is not CraftingResourceEntry resource)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingSellPriceOption priceOption)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.SelectResourcePriceOption(resource, priceOption);
        listBox.SelectedItem = null;
    }

    private void ListBoxJournalEmptyPrice_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingSellPriceOption priceOption)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.SelectJournalEmptyPriceOption(priceOption);
        listBox.SelectedItem = null;
    }

    private void ListBoxJournalFullPrice_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingSellPriceOption priceOption)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.SelectJournalFullPriceOption(priceOption);
        listBox.SelectedItem = null;
    }

    private void CopyTextBlockTextToClipboard_OnMouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
    {
        if (sender is not TextBlock { Text: { } textBlockText }
            || string.IsNullOrWhiteSpace(textBlockText))
        {
            return;
        }

        System.Windows.Clipboard.SetText(textBlockText);
        e.Handled = true;
    }

    private void CraftingLocationSearch_OnGotKeyboardFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenCraftingLocationSearch();
    }

    private void OutputUnitPrice_OnGotKeyboardFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenSellPriceOptions();
    }

    private void PriceTextBox_OnLostKeyboardFocus(object sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var newFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;

            if ((sender is Visual textBox && IsElementOrChildOf(newFocus, textBox))
                || IsPriceOptionPopupClick(newFocus))
            {
                return;
            }

            CloseSellPriceOptions();
        });
    }

    private void OutputUnitPrice_OnPreviewMouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
    {
        if (!ShouldOpenPricePopupOnMouseLeftButtonUp(sender))
        {
            return;
        }

        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenSellPriceOptions();
    }

    private void ResourcePrice_OnGotKeyboardFocus(object sender, RoutedEventArgs e)
    {
        OpenResourcePriceOptions(sender);
    }

    private void ResourcePrice_OnPreviewMouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
    {
        if (!ShouldOpenPricePopupOnMouseLeftButtonUp(sender))
        {
            return;
        }

        OpenResourcePriceOptions(sender);
    }

    private void JournalEmptyPrice_OnGotKeyboardFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenJournalEmptyPriceOptions();
    }

    private void JournalEmptyPrice_OnPreviewMouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
    {
        if (!ShouldOpenPricePopupOnMouseLeftButtonUp(sender))
        {
            return;
        }

        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenJournalEmptyPriceOptions();
    }

    private void JournalFullPrice_OnGotKeyboardFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenJournalFullPriceOptions();
    }

    private void JournalFullPrice_OnPreviewMouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
    {
        if (!ShouldOpenPricePopupOnMouseLeftButtonUp(sender))
        {
            return;
        }

        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenJournalFullPriceOptions();
    }

    private void PriceTextBox_OnPreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
    {
        _wasPriceTextBoxFocusedOnMouseDown = sender is Control frameworkElement
                                            && frameworkElement.IsKeyboardFocusWithin;
    }

    private void CraftingControl_OnPreviewMouseDown(object sender, PointerPressedEventArgs e)
    {
        var source = e.Source as Visual;

        if (!IsElementOrChildOf(source, CraftingItemSearchTextBox)
            && !IsItemSearchPopupClick(source))
        {
            CloseItemSearch();
        }

        if (IsElementOrChildOf(source, OutputUnitPriceTextBox)
            || IsPriceOptionPopupClick(source))
        {
            return;
        }

        CloseSellPriceOptions();
    }

    private void FilterReset_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.BlackMarket?.ResetFilters();
    }

    private void CraftingItemFilterReset_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.ResetItemFilters();
    }

    private void OpenResourcePriceOptions(object sender)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not Control frameworkElement)
        {
            return;
        }

        if (frameworkElement.DataContext is not CraftingResourceEntry resource)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenResourcePriceOptions(resource);
    }

    private bool ShouldOpenPricePopupOnMouseLeftButtonUp(object sender)
    {
        var shouldOpen = sender is Control frameworkElement
                         && frameworkElement.IsKeyboardFocusWithin
                         && _wasPriceTextBoxFocusedOnMouseDown;

        _wasPriceTextBoxFocusedOnMouseDown = false;

        return shouldOpen;
    }

    private void CloseSellPriceOptions()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.CloseAllPriceOptionPopups();
    }

    private void CloseItemSearch()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.CloseItemSearch();
    }

    private static bool IsElementOrChildOf(Visual source, Visual parent)
    {
        while (source != null)
        {
            if (ReferenceEquals(source, parent))
            {
                return true;
            }

            source = GetParent(source);
        }

        return false;
    }

    private static bool IsItemSearchPopupClick(Visual source)
    {
        while (source != null)
        {
            if (source is Control { DataContext: CraftingItemSearchResult })
            {
                return true;
            }

            if (source is ListBox listBox
                && listBox.Items.Count > 0
                && listBox.Items[0] is CraftingItemSearchResult)
            {
                return true;
            }

            source = GetParent(source);
        }

        return false;
    }

    private static bool IsPriceOptionPopupClick(Visual source)
    {
        while (source != null)
        {
            if (source is Control { DataContext: CraftingSellPriceOption })
            {
                return true;
            }

            if (source is ListBox listBox
                && listBox.Items.Count > 0
                && listBox.Items[0] is CraftingSellPriceOption)
            {
                return true;
            }

            source = GetParent(source);
        }

        return false;
    }

    private static Visual GetParent(Visual source) => source.GetVisualParent();
}
