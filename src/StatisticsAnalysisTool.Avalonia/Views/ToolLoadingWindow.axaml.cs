using Avalonia.Input;
using StatisticsAnalysisTool.ViewModels;

namespace StatisticsAnalysisTool.Views;

/// <summary>Startup progress bar. Port of Views/ToolLoadingWindow.xaml.</summary>
public partial class ToolLoadingWindow : System.Windows.Window
{
    public ToolLoadingWindow() : this(new ToolLoadingWindowViewModel())
    {
    }

    public ToolLoadingWindow(ToolLoadingWindowViewModel toolLoadingWindowViewModel)
    {
        InitializeComponent();
        DataContext = toolLoadingWindowViewModel;
        Hotbar.PointerPressed += Hotbar_PointerPressed;
    }

    private void Hotbar_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
