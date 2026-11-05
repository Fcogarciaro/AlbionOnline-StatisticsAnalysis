using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.ViewModels;
using System.Windows;
using AvaloniaWindowState = Avalonia.Controls.WindowState;

namespace StatisticsAnalysisTool.Views;

/// <summary>Port of Views/DashboardWindow.xaml(.cs); hosts the placeholder until DashboardControl is ported (T13).</summary>
public partial class DashboardWindow : System.Windows.Window
{
    private readonly WindowChromeController _windowChromeController;

    public DashboardWindow(MainWindowViewModel mainWindowViewModel)
    {
        InitializeComponent();
        _windowChromeController = new WindowChromeController(
            this,
            MaximizedButton,
            ResizeMode.CanResizeWithGrip,
            centerOnRestore: true);
        ResizeMode = ResizeMode.CanResizeWithGrip;
        DataContext = mainWindowViewModel;
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
