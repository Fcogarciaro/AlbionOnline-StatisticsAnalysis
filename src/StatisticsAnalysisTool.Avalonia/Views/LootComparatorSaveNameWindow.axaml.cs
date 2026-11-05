using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Localization;
using System.Windows;

namespace StatisticsAnalysisTool.Views;

/// <summary>Port of Views/LootComparatorSaveNameWindow.xaml(.cs).</summary>
public partial class LootComparatorSaveNameWindow : System.Windows.Window
{
    private readonly HotbarDragHandler _hotbarDragHandler;

    public LootComparatorSaveNameWindow()
    {
        InitializeComponent();
        _hotbarDragHandler = new HotbarDragHandler(this);

        Title = LocalizationController.Translation("SAVE_LOOT_COMPARATOR_STATE");
        TitleLabel.Content = Title;
        PromptTextBlock.Text = LocalizationController.Translation("LOOT_COMPARATOR_SAVE_NAME_PROMPT");
        SaveButton.Content = LocalizationController.Translation("SAVE");
        CancelButton.Content = LocalizationController.Translation("CANCEL");
        SaveButton.IsEnabled = false;

        Opened += (_, _) => SaveNameTextBox.Focus();
    }

    public string SaveName { get; private set; } = string.Empty;

    private void SaveNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SaveButton.IsEnabled = !string.IsNullOrWhiteSpace(SaveNameTextBox.Text);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var name = SaveNameTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        SaveName = name;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Hotbar_PointerPressed(object sender, PointerPressedEventArgs e) => _hotbarDragHandler.OnPointerPressed(e);
}
