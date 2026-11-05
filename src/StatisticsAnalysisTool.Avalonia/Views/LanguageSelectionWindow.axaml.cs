using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.ViewModels;

namespace StatisticsAnalysisTool.Views;

/// <summary>Port of Views/LanguageSelectionWindow.xaml(.cs).</summary>
public partial class LanguageSelectionWindow : System.Windows.Window
{
    private readonly LanguageSelectionWindowViewModel _languageSelectionWindowViewModel;
    private readonly HotbarDragHandler _hotbarDragHandler;

    public LanguageSelectionWindow()
    {
        InitializeComponent();
        _hotbarDragHandler = new HotbarDragHandler(this);
        _languageSelectionWindowViewModel = new LanguageSelectionWindowViewModel();
        DataContext = _languageSelectionWindowViewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Hotbar_PointerPressed(object sender, PointerPressedEventArgs e) => _hotbarDragHandler.OnPointerPressed(e);

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_languageSelectionWindowViewModel.SelectedFileInformation?.FileName))
        {
            return;
        }

        DialogResult = true;
        Close();
    }
}
