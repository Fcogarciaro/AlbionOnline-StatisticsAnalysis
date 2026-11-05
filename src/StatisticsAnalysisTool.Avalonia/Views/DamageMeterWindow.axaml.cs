using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.DamageMeter;
using StatisticsAnalysisTool.ViewModels;
using System.Windows;
using AvaloniaWindowState = Avalonia.Controls.WindowState;

namespace StatisticsAnalysisTool.Views;

/// <summary>Port of Views/DamageMeterWindow.xaml(.cs); the list content is a placeholder until T14.</summary>
public partial class DamageMeterWindow : System.Windows.Window
{
    private readonly WindowChromeController _windowChromeController;

    public DamageMeterWindow(DamageMeterBindings damageMeterBindings)
    {
        InitializeComponent();
        _windowChromeController = new WindowChromeController(
            this,
            MaximizedButton,
            ResizeMode.CanResizeWithGrip,
            centerOnRestore: true);
        ResizeMode = ResizeMode.CanResizeWithGrip;
        DataContext = new DamageMeterWindowViewModel(damageMeterBindings);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = AvaloniaWindowState.Minimized;

    private void MaximizedButton_Click(object sender, RoutedEventArgs e)
    {
        _windowChromeController.ToggleMaximize();
    }

    private void Hotbar_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        _windowChromeController.ToggleMaximizeOnDoubleClick(e);
        if (!e.Handled)
        {
            _windowChromeController.DragMoveOnMouseDown(e);
        }
    }
}
