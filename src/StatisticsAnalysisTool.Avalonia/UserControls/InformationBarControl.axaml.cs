using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.HintBar;
using StatisticsAnalysisTool.ViewModels;
using System.Windows;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>Port of UserControls/InformationBarControl.xaml.</summary>
public partial class InformationBarControl : UserControl
{
    public static readonly StyledProperty<string> InformationTextProperty = AvaloniaProperty.Register<InformationBarControl, string>(nameof(InformationText));

    public InformationBarControl()
    {
        InitializeComponent();
    }

    public string InformationText
    {
        get => GetValue(InformationTextProperty);
        set => SetValue(InformationTextProperty, value);
    }

    private void BtnInformationBar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainWindowViewModel)
        {
            mainWindowViewModel.InformationBarText = string.Empty;
            mainWindowViewModel.InformationBarVisibility = Visibility.Collapsed;
        }
    }

    private void CopyToClipboard_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        HintBarClipboard.Copy("Information", InformationText);
    }
}
