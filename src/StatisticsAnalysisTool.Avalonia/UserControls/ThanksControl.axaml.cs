using Avalonia.Controls;
using StatisticsAnalysisTool.ViewModels;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>Port of UserControls/ThanksControl.xaml(.cs): same texts, own <see cref="ThanksViewModel"/>.</summary>
public partial class ThanksControl : UserControl
{
    public ThanksControl()
    {
        InitializeComponent();
        DataContext = new ThanksViewModel();
    }
}
