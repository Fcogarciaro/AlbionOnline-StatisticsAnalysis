using Avalonia.Controls;
using Avalonia.Input;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.ViewModels;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>
/// Port of UserControls/DonationControl.xaml(.cs). The <c>Hyperlink</c>s around the donation logos are clickable grids
/// (class "link", URL in Tag) opened through xdg-open; only http(s) URLs are opened.
/// </summary>
public partial class DonationControl : UserControl
{
    public DonationControl()
    {
        InitializeComponent();
        DataContext = new DonationViewModel();
    }

    private void Link_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is Control { Tag: string url } && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ExternalLink.Open(url);
        }
    }
}
