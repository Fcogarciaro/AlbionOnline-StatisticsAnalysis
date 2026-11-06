using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.ViewModels;
using AvaloniaWindowState = Avalonia.Controls.WindowState;

namespace StatisticsAnalysisTool.Views;

/// <summary>Port of Views/ItemAlertWindow.xaml(.cs).</summary>
public partial class ItemAlertWindow : System.Windows.Window
{
    private readonly HotbarDragHandler _hotbarDragHandler;

    public ItemAlertWindow(AlertInfos alertInfos)
    {
        InitializeComponent();
        _hotbarDragHandler = new HotbarDragHandler(this, toggleMaximizeOnDoubleClick: true);
        var itemAlertWindowViewModel = new ItemAlertWindowViewModel(alertInfos);
        DataContext = itemAlertWindowViewModel;
        CityNameTextBlock.Background = CreateCityBackground(itemAlertWindowViewModel.CityColor);
    }

    /// <summary>The WPF style: a horizontal gradient transparent - city color - transparent (from the right to the left).</summary>
    internal static IBrush CreateCityBackground(Color cityColor)
    {
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            GradientStops =
            [
                new GradientStop(Colors.Transparent, 0),
                new GradientStop(cityColor, 0.5),
                new GradientStop(Colors.Transparent, 1)
            ]
        };
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = AvaloniaWindowState.Minimized;

    private void Hotbar_PointerPressed(object sender, PointerPressedEventArgs e) => _hotbarDragHandler.OnPointerPressed(e);
}
