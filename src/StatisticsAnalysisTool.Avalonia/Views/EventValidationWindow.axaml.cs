using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.EventValidations;
using StatisticsAnalysisTool.ViewModels;
using System.Windows;
using AvaloniaWindowState = Avalonia.Controls.WindowState;

namespace StatisticsAnalysisTool.Views;

/// <summary>Port of Views/EventValidationWindow.xaml(.cs).</summary>
public partial class EventValidationWindow : System.Windows.Window
{
    private readonly WindowChromeController _windowChromeController;

    public EventValidationWindow()
    {
        InitializeComponent();
        _windowChromeController = new WindowChromeController(
            this,
            MaximizedButton,
            ResizeMode.CanResizeWithGrip,
            centerOnRestore: true);
        ResizeMode = ResizeMode.CanResizeWithGrip;
        DataContext = new EventValidationViewModel();
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

    private void BtnResetConsole_Click(object sender, RoutedEventArgs e)
    {
        EventValidator.Reset();
    }
}
