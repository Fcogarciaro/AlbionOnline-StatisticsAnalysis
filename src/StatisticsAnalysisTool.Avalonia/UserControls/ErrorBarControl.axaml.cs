using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.HintBar;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Windows;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>Port of UserControls/ErrorBarControl.xaml.</summary>
public partial class ErrorBarControl : UserControl
{
    public static readonly StyledProperty<string> ErrorTextProperty = AvaloniaProperty.Register<ErrorBarControl, string>(nameof(ErrorText));

    public static readonly StyledProperty<Exception> ErrorExceptionProperty = AvaloniaProperty.Register<ErrorBarControl, Exception>(nameof(ErrorException));

    public ErrorBarControl()
    {
        InitializeComponent();
    }

    public string ErrorText
    {
        get => GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public Exception ErrorException
    {
        get => GetValue(ErrorExceptionProperty);
        set => SetValue(ErrorExceptionProperty, value);
    }

    private void BtnErrorBar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainWindowViewModel)
        {
            mainWindowViewModel.ErrorBarText = string.Empty;
            mainWindowViewModel.ErrorBarException = null;
            mainWindowViewModel.ErrorBarVisibility = Visibility.Collapsed;
            return;
        }

        if (DataContext is ItemDetailsViewModel itemDetailsViewModel)
        {
            itemDetailsViewModel.ErrorBarText = string.Empty;
            itemDetailsViewModel.ErrorBarException = null;
            itemDetailsViewModel.ErrorBarVisibility = Visibility.Collapsed;
        }
    }

    private void CopyToClipboard_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        HintBarClipboard.Copy("Error", ErrorText, ErrorException);
    }
}
