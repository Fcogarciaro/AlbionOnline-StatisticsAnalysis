using System.Linq;
using System.Threading.Tasks;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Network.Manager;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;
using System.Windows;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.Guild;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>
/// Port of UserControls/GuildControl.xaml(.cs).
/// </summary>
public partial class GuildControl : UserControl
{
    private bool _isSelectAllActive;

    public GuildControl()
    {
        InitializeComponent();
    }

    private async void BtnDeleteSelectedSiphonedEnergyEntries_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DialogWindow(LocalizationController.Translation("DELETE_SELECTED_ENTRIES"), LocalizationController.Translation("SURE_YOU_WANT_TO_DELETE_SELECTED_ENTRIES"));
        var dialogResult = dialog.ShowDialog();

        var mainWindowViewModel = ServiceLocator.Resolve<MainWindowViewModel>();

        if (mainWindowViewModel == null)
        {
            return;
        }

        if (dialogResult is true)
        {
            var trackingController = ServiceLocator.Resolve<TrackingController>();

            var selectedEntries = mainWindowViewModel.GuildBindings.SiphonedEnergyCollectionView?.Cast<SiphonedEnergyItem>()
                .ToList()
                .Where(x => x?.IsSelectedForDeletion ?? false)
                .Select(x => x.GetHashCode());

            mainWindowViewModel.GuildBindings.IsDeleteEntriesButtonEnabled = false;
            await trackingController?.GuildController?.RemoveTradesByIdsAsync(selectedEntries)!;
            mainWindowViewModel.GuildBindings.IsDeleteEntriesButtonEnabled = true;
        }
    }

    private void OpenSiphonedEnergyInfoPopup_MouseEnter(object sender, PointerEventArgs e)
    {
        var vm = (MainWindowViewModel) DataContext;
        vm.GuildBindings.GuildPopupVisibility = Visibility.Visible;
    }

    private void CloseSiphonedEnergyInfoPopup_MouseLeave(object sender, PointerEventArgs e)
    {
        var vm = (MainWindowViewModel) DataContext;
        vm.GuildBindings.GuildPopupVisibility = Visibility.Collapsed;
    }

    private void BtnSelectSwitchAllSiphonedEnergyEntries_Click(object sender, RoutedEventArgs e)
    {
        if ((MainWindowViewModel) DataContext is not { GuildBindings.SiphonedEnergyList: not null } mainWindowViewModel)
        {
            return;
        }

        foreach (var item in mainWindowViewModel.GuildBindings.SiphonedEnergyList)
        {
            item.IsSelectedForDeletion = !_isSelectAllActive;
        }

        _isSelectAllActive = !_isSelectAllActive;
    }
}