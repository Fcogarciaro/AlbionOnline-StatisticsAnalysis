using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>
/// Port of UserControls/ItemSearchControl.xaml(.cs). The WPF GridView is a DataGrid (template columns); its column widths are
/// restored from and written back to the settings like upstream (<c>DependencyPropertyDescriptor</c> on the column width
/// becomes an observable on <see cref="DataGridColumn.WidthProperty"/>). The preview click (PreviewMouseLeftButtonDown) is a
/// tunnelling PointerPressed handler.
/// </summary>
public partial class ItemSearchControl : UserControl
{
    private const double DefaultIconColumnWidth = 75;
    private const double DefaultNameColumnWidth = 215;
    private const double DefaultFavoriteColumnWidth = 55;
    private readonly List<IDisposable> _columnWidthSubscriptions = [];

    // Column definitions and DataGrid columns are not controls (no x:Name): they are taken by index.
    private ColumnDefinition ItemListColumn { get; }
    private ColumnDefinition ItemSearchSplitterColumn { get; }
    private ColumnDefinition ItemDetailsColumn { get; }
    private DataGridColumn ItemIconColumn { get; }
    private DataGridColumn ItemNameColumn { get; }
    private DataGridColumn ItemFavoriteColumn { get; }

    public ItemSearchControl()
    {
        InitializeComponent();
        ItemListColumn = ItemSearchLayoutGrid.ColumnDefinitions[0];
        ItemSearchSplitterColumn = ItemSearchLayoutGrid.ColumnDefinitions[1];
        ItemDetailsColumn = ItemSearchLayoutGrid.ColumnDefinitions[2];
        ItemIconColumn = ItemSearchList.Columns[0];
        ItemNameColumn = ItemSearchList.Columns[1];
        ItemFavoriteColumn = ItemSearchList.Columns[2];
        Loaded += ItemSearchControl_Loaded;
        Unloaded += ItemSearchControl_Unloaded;
        SizeChanged += (_, _) => UpdateItemListMaximumWidth();
        ItemSearchList.AddHandler(PointerPressedEvent, ItemSearchList_PreviewPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private void UpdateItemListMaximumWidth()
    {
        var maximumItemListWidth = ItemSearchLayoutGrid.Bounds.Width - ItemSearchSplitterColumn.ActualWidth - ItemDetailsColumn.MinWidth;
        ItemListColumn.MaxWidth = maximumItemListWidth >= ItemListColumn.MinWidth
            ? maximumItemListWidth
            : ItemListColumn.MinWidth;
    }

    private void ItemSearchControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_columnWidthSubscriptions.Count > 0)
        {
            return;
        }

        ApplySavedColumnWidths();
        foreach (var column in new[] { ItemIconColumn, ItemNameColumn, ItemFavoriteColumn })
        {
            _columnWidthSubscriptions.Add(column.GetObservable(DataGridColumn.WidthProperty).Subscribe(new ColumnWidthObserver(ItemSearchColumnWidthChanged)));
        }
    }

    private void ItemSearchControl_Unloaded(object sender, RoutedEventArgs e)
    {
        foreach (var subscription in _columnWidthSubscriptions)
        {
            subscription.Dispose();
        }

        _columnWidthSubscriptions.Clear();
    }

    private void ApplySavedColumnWidths()
    {
        var settings = SettingsController.CurrentSettings;
        ItemIconColumn.Width = new DataGridLength(GetValidColumnWidth(settings.ItemSearchIconColumnWidth, DefaultIconColumnWidth));
        ItemNameColumn.Width = new DataGridLength(GetValidColumnWidth(settings.ItemSearchNameColumnWidth, DefaultNameColumnWidth));
        ItemFavoriteColumn.Width = new DataGridLength(GetValidColumnWidth(settings.ItemSearchFavoriteColumnWidth, DefaultFavoriteColumnWidth));
    }

    private void ItemSearchColumnWidthChanged()
    {
        var settings = SettingsController.CurrentSettings;
        settings.ItemSearchIconColumnWidth = GetPersistableColumnWidth(ItemIconColumn, DefaultIconColumnWidth);
        settings.ItemSearchNameColumnWidth = GetPersistableColumnWidth(ItemNameColumn, DefaultNameColumnWidth);
        settings.ItemSearchFavoriteColumnWidth = GetPersistableColumnWidth(ItemFavoriteColumn, DefaultFavoriteColumnWidth);
    }

    internal static double GetValidColumnWidth(double columnWidth, double defaultWidth)
    {
        return double.IsNaN(columnWidth) || double.IsInfinity(columnWidth) || columnWidth < 0
            ? defaultWidth
            : columnWidth;
    }

    private static double GetPersistableColumnWidth(DataGridColumn column, double defaultWidth)
    {
        var width = column.Width.IsAbsolute ? column.Width.Value : double.NaN;
        return double.IsNaN(width) || double.IsInfinity(width) || width < 0
            ? GetValidColumnWidth(column.ActualWidth, defaultWidth)
            : width;
    }

    #region Ui events

    private async void ItemSearchList_PreviewPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is not DataGrid itemSearchList || !e.GetCurrentPoint(itemSearchList).Properties.IsLeftButtonPressed || e.Source is not Visual source)
        {
            return;
        }

        var row = source.FindAncestorOfType<DataGridRow>(includeSelf: true);
        if (row?.DataContext is not Item item || !ReferenceEquals(itemSearchList.SelectedItem, item))
        {
            return;
        }

        if (e.GetPosition(row).X > ItemIconColumn.ActualWidth + ItemNameColumn.ActualWidth)
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.RefreshSelectedItemOnOpeningAsync(item);
        }
    }

    private void FilterReset_Click(object sender, RoutedEventArgs e)
    {
        var vm = (MainWindowViewModel) DataContext;
        vm?.ItemFilterReset();
    }

    private void OpenItemSearchInfoPopup_MouseEnter(object sender, PointerEventArgs e)
    {
        var vm = (MainWindowViewModel) DataContext;
        vm.IsItemSearchPopupVisible = Visibility.Visible;
    }

    private void CloseItemSearchInfoPopup_MouseLeave(object sender, PointerEventArgs e)
    {
        var vm = (MainWindowViewModel) DataContext;
        vm.IsItemSearchPopupVisible = Visibility.Hidden;
    }

    #endregion

    private sealed class ColumnWidthObserver(Action onChanged) : IObserver<DataGridLength>
    {
        public void OnNext(DataGridLength value) => onChanged();

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
