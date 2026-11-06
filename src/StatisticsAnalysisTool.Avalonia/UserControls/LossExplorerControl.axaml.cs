using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.ViewModels;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>
/// Port of UserControls/LossExplorerControl.xaml(.cs). WPF <c>IsVisibleChanged</c> (to true) loads the data; a TabControl only keeps
/// the selected content in the tree, so the load runs when the control is loaded and when it becomes visible again.
/// </summary>
public partial class LossExplorerControl : UserControl
{
    public LossExplorerControl()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadWhenVisible();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty && change.NewValue is true)
        {
            LoadWhenVisible();
        }
    }

    private async void LoadWhenVisible()
    {
        if (!IsLoaded || !IsEffectivelyVisible || DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        var lossExplorer = mainWindowViewModel.CraftingBindings.LossExplorer;
        if (lossExplorer != null)
        {
            await lossExplorer.LoadAsync();
        }
    }

    private void LossExplorerFilterReset_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainWindowViewModel)
        {
            mainWindowViewModel.CraftingBindings.LossExplorer?.ResetFilters();
        }
    }
}
