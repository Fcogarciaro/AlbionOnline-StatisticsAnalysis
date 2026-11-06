using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.Models.BindingModel;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.ComponentModel;
using System.Windows.Data;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>
/// Port of UserControls/ItemDetailsControl.xaml(.cs). The main tab price list is a DataGrid (no ListView + GridView): its header click is
/// the DataGrid <c>Sorting</c> event (columns carry the sort property in <c>Tag</c>), and the grid shows the default collection view of the
/// price list so the sort is visible, like the WPF ListView did.
/// </summary>
public partial class ItemDetailsControl : UserControl
{
    private ListSortDirection _lastDirection = ListSortDirection.Ascending;
    private ItemDetailsMainTabBindings _subscribedMainTabBindings;

    public ItemDetailsControl()
    {
        InitializeComponent();
        DataContextChanged += ItemDetailsControl_OnDataContextChanged;
    }

    private void ItemDetailsControl_OnDataContextChanged(object sender, EventArgs e)
    {
        if (_subscribedMainTabBindings != null)
        {
            _subscribedMainTabBindings.PropertyChanged -= MainTabBindings_OnPropertyChanged;
        }

        _subscribedMainTabBindings = (DataContext as ItemDetailsViewModel)?.MainTabBindings;
        if (_subscribedMainTabBindings != null)
        {
            _subscribedMainTabBindings.PropertyChanged += MainTabBindings_OnPropertyChanged;
        }

        ShowItemPrices();
    }

    private void MainTabBindings_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ItemDetailsMainTabBindings.ItemPrices) || string.IsNullOrEmpty(e.PropertyName))
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(ShowItemPrices);
        }
    }

    private void ShowItemPrices()
    {
        ItemPricesGrid.ItemsSource = _subscribedMainTabBindings == null
            ? null
            : (System.Collections.IEnumerable) CollectionViewSource.GetDefaultView(_subscribedMainTabBindings.ItemPrices);
    }

    private async void Refresh_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        e.Handled = true;

        if (DataContext is ItemDetailsViewModel viewModel)
        {
            await viewModel.RefreshManuallyAsync();
        }
    }

    private void PriceAlert_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ItemDetailsViewModel viewModel)
        {
            viewModel.TogglePriceAlert();
        }
    }

    private void AvailabilityAlert_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ItemDetailsViewModel viewModel)
        {
            viewModel.ToggleAvailabilityAlert();
        }
    }

    private void BlackMarketBuyOrderAlert_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ItemDetailsViewModel viewModel)
        {
            viewModel.ToggleBlackMarketBuyOrderAlert();
        }
    }

    private void ItemPricesGrid_OnSorting(object sender, DataGridColumnEventArgs e)
    {
        e.Handled = true;

        if (DataContext is not ItemDetailsViewModel viewModel || e.Column.Tag?.ToString() is not { } sortBy)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(viewModel.MainTabBindings.ItemPrices);
        view.SortDescriptions.Clear();

        _lastDirection = _lastDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        view.SortDescriptions.Add(new SortDescription(sortBy, _lastDirection));
        view.Refresh();
    }
}
