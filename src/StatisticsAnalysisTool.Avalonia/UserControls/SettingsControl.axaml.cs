using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Serilog;
using StatisticsAnalysisTool.Backup;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.Validation;
using StatisticsAnalysisTool.Diagnostics;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Updater;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace StatisticsAnalysisTool.UserControls;

/// <summary>
/// Port of UserControls/SettingsControl.xaml(.cs). Differences: folders open through xdg-open, the desktop shortcut is a
/// Desktop Entry (<see cref="DesktopShortcut"/>), the backup path shows its validation error as tooltip and red border
/// (class "invalid", <see cref="PathValidationRule"/>), and the settings are reloaded when the tab content becomes visible.
/// </summary>
public partial class SettingsControl : UserControl
{
    private readonly SettingsWindowViewModel _settingsWindowViewModel;

    public SettingsControl()
    {
        InitializeComponent();
        _settingsWindowViewModel = new SettingsWindowViewModel();
        DataContext = _settingsWindowViewModel;

        // WPF IsVisibleChanged of the root grid: reload whenever the tab is shown. A TabControl only keeps the selected
        // content in the visual tree, so (re)attaching it is the moment the tab becomes visible.
        AttachedToVisualTree += (_, _) => _settingsWindowViewModel.ReloadSettings();
        BackupStorageDirectoryPathTextBox.TextChanged += BackupStorageDirectoryPathTextBox_TextChanged;
        ValidateBackupStorageDirectoryPath();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        await _settingsWindowViewModel.SaveSettingsAsync();
    }

    private void OpenToolDirectory_Click(object sender, RoutedEventArgs e)
    {
        OpenDirectory(_settingsWindowViewModel.ToolDirectory);
    }

    private void OpenUserDataDirectory_Click(object sender, RoutedEventArgs e)
    {
        OpenDirectory(_settingsWindowViewModel.UserDataDirectory);
    }

    private static void OpenDirectory(string directoryPath)
    {
        try
        {
            Directory.CreateDirectory(directoryPath);

            if (!ExternalLink.OpenFolder(directoryPath))
            {
                throw new InvalidOperationException($"The folder could not be opened: {directoryPath}");
            }
        }
        catch (Exception ex)
        {
            _ = MessageBox.Show(ex.Message, LocalizationController.Translation("ERROR"));
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, ex);
            Log.Error(ex, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
        }
    }

    private void CreateDesktopShortcut_Click(object sender, RoutedEventArgs e)
    {
        var processPath = Environment.ProcessPath;
        var entryPath = Assembly.GetEntryAssembly()?.Location;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            return;
        }

        var isDotnetHost = string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.Ordinal);
        var startPath = isDotnetHost && !string.IsNullOrEmpty(entryPath) ? entryPath : processPath;
        var directory = Path.GetDirectoryName(startPath);
        var iconPath = Path.Combine(directory ?? string.Empty, "sat-icon.png");
        var desktop = Environment.GetEnvironmentVariable("XDG_DESKTOP_DIR");
        if (string.IsNullOrEmpty(desktop) || !Path.IsPathRooted(desktop))
        {
            desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
        }

        DesktopShortcut.TryCreate(
            DesktopShortcut.ResolveApplicationsDirectory(Environment.GetEnvironmentVariable),
            desktop,
            AutostartEntry.BuildExec(processPath, entryPath),
            directory,
            File.Exists(iconPath) ? iconPath : null);
    }

    private void OpenDebugConsole_Click(object sender, RoutedEventArgs e)
    {
        DebugConsole.Attach("SAT Debug Console");
        var args = _settingsWindowViewModel.DebugConsoleFilter;
        DebugConsole.Configure(args);
    }

    private void CloseDebugConsole_Click(object sender, RoutedEventArgs e)
    {
        DebugConsole.Detach();
    }

    private void OpenEventValidation_Click(object sender, RoutedEventArgs e)
    {
        SettingsWindowViewModel.OpenEventValidationWindow();
    }

    private async void CheckForUpdate_Click(object sender, RoutedEventArgs e)
    {
        await AutoUpdateController.CheckForUpdatesAsync();
    }

    private void ResetPlayerSelectionWithSameNameInDb_Click(object sender, RoutedEventArgs e)
    {
        _settingsWindowViewModel.ResetPlayerSelectionWithSameNameInDb();
    }

    private void ResetBackupStorageDirPath_Click(object sender, RoutedEventArgs e)
    {
        _settingsWindowViewModel.ResetBackupStorageDirPath();
    }

    private void ResetPacketFilter_Click(object sender, RoutedEventArgs e)
    {
        _settingsWindowViewModel.ResetPacketFilter();
    }

    private void MainGameFolderPathTextBox_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        _settingsWindowViewModel.OpenMainGameFolderPathSelection();
    }

    private async void RestartNetworkTracking_Click(object sender, RoutedEventArgs e)
    {
        await SettingsWindowViewModel.RestartNetworkTrackingAsync();
    }

    private async void TrackingStateButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton trackingStateButton)
        {
            return;
        }

        trackingStateButton.IsEnabled = false;
        try
        {
            await _settingsWindowViewModel.ToggleTrackingAsync();
        }
        finally
        {
            // WPF: GetBindingExpression(IsChecked).UpdateTarget() - show the real state after the click.
            trackingStateButton.SetCurrentValue(ToggleButton.IsCheckedProperty, _settingsWindowViewModel.MainWindow?.IsTrackingActive ?? false);
            trackingStateButton.IsEnabled = true;
        }
    }

    private void PreviewAlertSound_Click(object sender, RoutedEventArgs e)
    {
        _settingsWindowViewModel.PreviewAlertSound();
    }

    private void PreviewDeathAlertSound_Click(object sender, RoutedEventArgs e)
    {
        _settingsWindowViewModel.PreviewDeathAlertSound();
    }

    private async void BackupNow_Click(object sender, RoutedEventArgs e)
    {
        _settingsWindowViewModel.IsBackupNowButtonEnabled = false;
        BackupController.Save();
        await Task.Delay(200);
        _settingsWindowViewModel.IsBackupNowButtonEnabled = true;
    }

    private void BackupStorageDirectoryPathTextBox_TextChanged(object sender, TextChangedEventArgs e) => ValidateBackupStorageDirectoryPath();

    private void ValidateBackupStorageDirectoryPath()
    {
        var error = PathValidationRule.GetError(BackupStorageDirectoryPathTextBox.Text);
        ToolTip.SetTip(BackupStorageDirectoryPathTextBox, error);
        BackupStorageDirectoryPathTextBox.Classes.Set("invalid", error != null);
    }
}
