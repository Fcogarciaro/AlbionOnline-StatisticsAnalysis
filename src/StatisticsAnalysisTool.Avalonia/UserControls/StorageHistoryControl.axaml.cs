using Avalonia.Controls;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Network.Manager;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>Port of UserControls/StorageHistoryControl.xaml(.cs): same delete confirmation (modal dialog), same vault removal.</summary>
public partial class StorageHistoryControl : UserControl
{
    public StorageHistoryControl()
    {
        InitializeComponent();
    }

    private void ButtonDeleteCurrentStorage_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new DialogWindow(LocalizationController.Translation("DELETE_SELECTED_STORAGE"), LocalizationController.Translation("SURE_YOU_WANT_TO_DELETE_SELECTED_STORAGE"));
        var dialogResult = dialog.ShowDialog();

        var vm = (MainWindowViewModel) DataContext;

        if (dialogResult is true)
        {
            var trackingController = ServiceLocator.Resolve<TrackingController>();
            trackingController?.VaultController?.RemoveVault(vm.VaultBindings?.VaultSelected);
        }
    }
}
