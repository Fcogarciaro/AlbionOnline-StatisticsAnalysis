using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.HintBar;
using StatisticsAnalysisTool.ViewModels;
using System.Windows;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>Port of UserControls/WarningBarControl.xaml.</summary>
public partial class WarningBarControl : UserControl
{
    public static readonly StyledProperty<string> WarningTextProperty = AvaloniaProperty.Register<WarningBarControl, string>(nameof(WarningText));

    public WarningBarControl()
    {
        InitializeComponent();
    }

    public string WarningText
    {
        get => GetValue(WarningTextProperty);
        set => SetValue(WarningTextProperty, value);
    }

    private void BtnWarningBar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainWindowViewModel)
        {
            mainWindowViewModel.WarningBarText = string.Empty;
            mainWindowViewModel.WarningBarVisibility = Visibility.Collapsed;
        }
    }

    private void CopyToClipboard_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        HintBarClipboard.Copy("Warning", WarningText);
    }
}
