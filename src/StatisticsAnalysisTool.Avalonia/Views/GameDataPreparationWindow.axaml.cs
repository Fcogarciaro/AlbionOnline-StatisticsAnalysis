using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ookii.Dialogs.Wpf;
using StatisticAnalysisTool.Extractor;
using StatisticAnalysisTool.Extractor.Enums;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatisticsAnalysisTool.Views;

/// <summary>
/// Port of Views/GameDataPreparationWindow.xaml(.cs). Linux additions: the usual Steam and Wine install locations are probed
/// (<see cref="GameFolderLocator"/>) and offered as buttons (the first one is preselected, Confirm stays the user's decision),
/// and the folder picker opens at the most likely location. The validation is the same Extractor.IsValidMainGameFolder.
/// </summary>
public partial class GameDataPreparationWindow : System.Windows.Window
{
    private readonly GameDataPreparationWindowViewModel _gameDataPreparationWindowViewModel;
    private readonly HotbarDragHandler _hotbarDragHandler;
    private readonly Func<string, bool> _isValidMainGameFolder;
    private readonly Func<IReadOnlyList<string>> _detectFolders;
    private readonly Func<string> _suggestedStart;

    public GameDataPreparationWindow() : this(
        folder => Extractor.IsValidMainGameFolder(folder, ServerType.Live),
        () => GameFolderLocator.FindValid(Home, Environment.GetEnvironmentVariable, folder => Extractor.IsValidMainGameFolder(folder, ServerType.Live)).ToList(),
        () => GameFolderLocator.SuggestedStart(Home, Environment.GetEnvironmentVariable))
    {
    }

    /// <param name="isValidMainGameFolder">Validation of a picked folder; tests inject a fake.</param>
    /// <param name="detectFolders">Valid installations found automatically.</param>
    /// <param name="suggestedStart">Start folder of the picker.</param>
    internal GameDataPreparationWindow(Func<string, bool> isValidMainGameFolder, Func<IReadOnlyList<string>> detectFolders, Func<string> suggestedStart)
    {
        InitializeComponent();
        _hotbarDragHandler = new HotbarDragHandler(this);
        _isValidMainGameFolder = isValidMainGameFolder;
        _detectFolders = detectFolders;
        _suggestedStart = suggestedStart;
        _gameDataPreparationWindowViewModel = new GameDataPreparationWindowViewModel();
        DataContext = _gameDataPreparationWindowViewModel;
        ShowDetectedFolders();
    }

    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>The folder that Confirm will hand to the caller (empty until a valid folder was chosen).</summary>
    internal string SelectedFolder => _gameDataPreparationWindowViewModel.Path;

    private void ShowDetectedFolders()
    {
        IReadOnlyList<string> folders;
        try
        {
            folders = _detectFolders();
        }
        catch (Exception exception)
        {
            Serilog.Log.Warning(exception, "Detecting the Albion Online folder failed");
            return;
        }

        if (folders.Count == 0)
        {
            return;
        }

        DetectedLabel.Text = "Found on this computer:";
        DetectedFolders.ItemsSource = folders
            .Select(folder =>
            {
                var button = new Button { Content = folder, Tag = folder, Margin = new global::Avalonia.Thickness(0, 2), HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center };
                button.Click += DetectedFolder_Click;
                return button;
            })
            .ToList();
        DetectedPanel.IsVisible = true;
        ApplyFolder(folders[0]);
    }

    private void DetectedFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string folder })
        {
            _gameDataPreparationWindowViewModel.ErrorMessage = string.Empty;
            ApplyFolder(folder);
        }
    }

    private void ApplyFolder(string folder)
    {
        if (_isValidMainGameFolder(folder ?? string.Empty))
        {
            _gameDataPreparationWindowViewModel.Path = folder;
            _gameDataPreparationWindowViewModel.IsConfirmButtonEnabled = true;
        }
        else
        {
            _gameDataPreparationWindowViewModel.ErrorMessage = LocalizationController.Translation("PLEASE_SELECT_A_CORRECT_FOLDER");
            _gameDataPreparationWindowViewModel.IsConfirmButtonEnabled = false;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Hotbar_PointerPressed(object sender, PointerPressedEventArgs e) => _hotbarDragHandler.OnPointerPressed(e);

    private void BtnSelectMainGameFolder_Click(object sender, RoutedEventArgs e)
    {
        _gameDataPreparationWindowViewModel.ErrorMessage = string.Empty;

        var dialog = new VistaFolderBrowserDialog
        {
            Description = LocalizationController.Translation("SELECT_ALBION_ONLINE_MAIN_GAME_FOLDER"),
            SelectedPath = _suggestedStart(),
            ShowNewFolderButton = false,
            UseDescriptionForTitle = true,
            Multiselect = false
        };

        var result = dialog.ShowDialog();
        if (result.HasValue && result.Value)
        {
            ApplyFolder(dialog.SelectedPath);
        }
    }

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
