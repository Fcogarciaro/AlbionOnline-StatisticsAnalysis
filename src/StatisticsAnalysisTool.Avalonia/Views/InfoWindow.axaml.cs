using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.ViewModels;
using AvaloniaWindowState = Avalonia.Controls.WindowState;

namespace StatisticsAnalysisTool.Views;

/// <summary>Port of Views/InfoWindow.xaml(.cs). The Npcap link of step 2 points to the capabilities(7) man page on Linux.</summary>
public partial class InfoWindow : System.Windows.Window
{
    private readonly HotbarDragHandler _hotbarDragHandler;

    public InfoWindow()
    {
        InitializeComponent();
        _hotbarDragHandler = new HotbarDragHandler(this, toggleMaximizeOnDoubleClick: true);
        var infoWindowViewModel = new InfoWindowViewModel();
        DataContext = infoWindowViewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = AvaloniaWindowState.Minimized;

    private void Hotbar_PointerPressed(object sender, PointerPressedEventArgs e) => _hotbarDragHandler.OnPointerPressed(e);

    private void Hyperlink_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is Control { Tag: string url } && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ExternalLink.Open(url);
            e.Handled = true;
        }
    }
}
