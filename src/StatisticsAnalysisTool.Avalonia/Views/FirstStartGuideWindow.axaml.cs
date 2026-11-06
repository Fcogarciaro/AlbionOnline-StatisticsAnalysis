using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ookii.Dialogs.Wpf;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Linux.Capture;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.Linux.Startup;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;

namespace StatisticsAnalysisTool.Views;

/// <summary>
/// Port of Views/FirstStartGuideWindow.xaml(.cs). Linux differences: the tracking step offers libpcap (Sockets is mapped to it,
/// texts from <see cref="CaptureGuidance"/>), the Npcap download link opens the capabilities(7) man page, the setcap guidance
/// is shown whenever the process lacks CAP_NET_RAW, and the game folder step pre-fills a detected Steam/Wine install and opens
/// the picker at the most likely location. Completing persists HasCompletedFirstStartGuide through the unchanged view model.
/// </summary>
public partial class FirstStartGuideWindow : System.Windows.Window
{
    private readonly FirstStartGuideViewModel _viewModel;
    private readonly HotbarDragHandler _hotbarDragHandler;
    private readonly CaptureEnvironment _captureEnvironment;

    public FirstStartGuideWindow() : this(CaptureEnvironment.Current, detectGameFolder: true)
    {
    }

    internal FirstStartGuideWindow(CaptureEnvironment captureEnvironment, bool detectGameFolder)
    {
        InitializeComponent();
        _hotbarDragHandler = new HotbarDragHandler(this);
        _captureEnvironment = captureEnvironment;
        _viewModel = new FirstStartGuideViewModel();
        _viewModel.Completed += ViewModel_Completed;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        DataContext = _viewModel;

        if (detectGameFolder)
        {
            PrefillDetectedGameFolder();
        }

        UpdateCapturePermissionPanel();
    }

    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private void PrefillDetectedGameFolder()
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.MainGameFolderPath))
        {
            return;
        }

        var found = GameFolderLocator.FindValid(Home, Environment.GetEnvironmentVariable, IsValidMainGameFolder).FirstOrDefault();
        if (found != null)
        {
            _viewModel.MainGameFolderPath = found;
        }
    }

    private static bool IsValidMainGameFolder(string path) =>
        StatisticAnalysisTool.Extractor.Extractor.IsValidMainGameFolder(path, SettingsController.CurrentSettings.ServerType);

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FirstStartGuideViewModel.NpcapWarningText) or nameof(FirstStartGuideViewModel.CurrentStepIndex) or null or "")
        {
            UpdateCapturePermissionPanel();
        }
    }

    private void UpdateCapturePermissionPanel()
    {
        var message = CaptureGuidance.PermissionMessage(_captureEnvironment.ProcessPath, _captureEnvironment.EntryAssemblyPath);
        var warningAlreadyCarriesIt = _viewModel.NpcapWarningText?.Contains("setcap", StringComparison.Ordinal) == true;
        CapturePermissionText.Text = message;
        CapturePermissionPanel.IsVisible = !_captureEnvironment.IsCapable && !warningAlreadyCarriesIt;
    }

    private void ViewModel_Completed(object sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Hotbar_PointerPressed(object sender, PointerPressedEventArgs e) => _hotbarDragHandler.OnPointerPressed(e);

    private void BtnSelectMainGameFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = LocalizationController.Translation("SELECT_ALBION_ONLINE_MAIN_GAME_FOLDER"),
            SelectedPath = GameFolderLocator.SuggestedStart(Home, Environment.GetEnvironmentVariable),
            ShowNewFolderButton = false,
            UseDescriptionForTitle = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() is true && !string.IsNullOrEmpty(dialog.SelectedPath))
        {
            _viewModel.MainGameFolderPath = dialog.SelectedPath;
            // The setter validates; Next stays disabled for a folder without game data (as upstream).
            if (!IsValidMainGameFolder(dialog.SelectedPath))
            {
                _viewModel.ErrorMessage = LocalizationController.Translation("PLEASE_SELECT_A_CORRECT_FOLDER");
            }
            else
            {
                _viewModel.ErrorMessage = string.Empty;
            }
        }
    }

    private void Link_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is Control { Tag: string url } && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ExternalLink.Open(url);
            e.Handled = true;
        }
    }

    private void CapabilitiesLink_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ExternalLink.Open(CaptureStartupDialog.InfoUrl);
            e.Handled = true;
        }
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: FirstStartGuideLanguageOption languageOption })
        {
            _viewModel.SelectLanguage(languageOption);
        }
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Back();
    }

    private async void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.NextAsync();
    }

    private async void BtnFinish_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.FinishAsync();
    }
}
