using Avalonia.Input;
using Avalonia.Interactivity;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace StatisticsAnalysisTool.Views;

/// <summary>
/// Port of Views/UpdateWindow.xaml(.cs). Same constructor (view model and the download/install action) so the updater can
/// show it. The Linux updater (Linux/Updater/AutoUpdateController.cs) does not report updates yet; when it does, its action
/// has no installer to run and should point to the release page instead (PORTING.md, updater row).
/// </summary>
public partial class UpdateWindow : System.Windows.Window
{
    private readonly Func<Task<bool>> _downloadInstallAction;
    private readonly HotbarDragHandler _hotbarDragHandler;

    public UpdateWindowViewModel UpdateWindowViewModel { get; }

    public UpdateWindow(UpdateWindowViewModel viewModel, Func<Task<bool>> downloadInstallAction)
    {
        InitializeComponent();
        _hotbarDragHandler = new HotbarDragHandler(this);

        UpdateWindowViewModel = viewModel;
        _downloadInstallAction = downloadInstallAction;
        DataContext = UpdateWindowViewModel;

        Height = UpdateWindowViewModel.HasPatchNotes ? 620 : 360;
        MinHeight = Height;
    }

    private async void DownloadInstall_Click(object sender, RoutedEventArgs e)
    {
        if (_downloadInstallAction == null)
        {
            DialogResult = true;
            Close();
            return;
        }

        var isSuccessful = await _downloadInstallAction();
        if (isSuccessful && IsVisible)
        {
            DialogResult = true;
            Close();
        }
    }

    private void RemindLater_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // Double clicks are swallowed upstream (no maximize); the handler ignores them too.
    private void Hotbar_PointerPressed(object sender, PointerPressedEventArgs e) => _hotbarDragHandler.OnPointerPressed(e);
}
