using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.ViewModels;

namespace StatisticsAnalysisTool.Views;

/// <summary>Port of Views/DialogWindow.xaml(.cs): yes/no, info and error dialog with an optional link.</summary>
public partial class DialogWindow : System.Windows.Window
{
    private const double DefaultDialogHeight = 200d;

    public readonly DialogWindowViewModel DialogWindowViewModel;
    private readonly HotbarDragHandler _hotbarDragHandler;

    public DialogWindow(string title, string message, DialogType type = DialogType.YesNo) : this(title, message, type, null)
    {
    }

    public DialogWindow(string title, string message, DialogType type, string url, string urlText = null, double? maxDynamicHeight = null)
    {
        InitializeComponent();
        _hotbarDragHandler = new HotbarDragHandler(this, toggleMaximizeOnDoubleClick: true);

        DialogWindowViewModel = new DialogWindowViewModel(title, message, type, url, urlText);
        DataContext = DialogWindowViewModel;

        if (maxDynamicHeight.HasValue)
        {
            MinHeight = DefaultDialogHeight;
            MaxHeight = maxDynamicHeight.Value;
            SizeToContent = SizeToContent.Height;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogWindowViewModel.Canceled = true;
        DialogResult = false;
        Close();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        DialogWindowViewModel.Canceled = false;
        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogWindowViewModel.Canceled = true;
        DialogResult = false;
        Close();
    }

    private void UrlTextBlock_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ExternalLink.Open(DialogWindowViewModel.Url);
            e.Handled = true;
        }
    }

    private void Hotbar_PointerPressed(object sender, PointerPressedEventArgs e) => _hotbarDragHandler.OnPointerPressed(e);
}
