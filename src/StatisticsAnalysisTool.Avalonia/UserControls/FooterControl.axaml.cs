using Avalonia.Controls;
using Avalonia.Input;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.ViewModels;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>Port of UserControls/FooterControl.xaml. Hyperlink elements become clickable grids that open the URL in Tag.</summary>
public partial class FooterControl : UserControl
{
    private readonly FooterViewModel _footerViewModel;

    public FooterControl()
    {
        InitializeComponent();
        _footerViewModel = new FooterViewModel();
        DataContext = _footerViewModel;
    }

    private void Link_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is Control { Tag: string url } && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ExternalLink.Open(url);
            e.Handled = true;
        }
    }

    private async void VersionLabel_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || !_footerViewModel.IsUpdateAvailable)
        {
            return;
        }

        e.Handled = true;
        await _footerViewModel.OpenUpdateWindowAsync();
    }
}
