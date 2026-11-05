// Port of App.OnStartup / OnExit / OnSessionEnding (App.xaml.cs), step for step and in the same order. Differences are
// listed in PORTING.md ("Startup flow"): lifetime events replace WPF's Exit/SessionEnding, the Npcap dialog becomes the
// capabilities guidance, and the first start guide, language selection and game data windows are still T10 placeholders.
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Serilog;
using StatisticAnalysisTool.Extractor.Enums;
using Serilog.Events;
using StatisticsAnalysisTool.Backup;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Diagnostics;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.GameFileData;
using StatisticsAnalysisTool.Linux.Capture;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Network;
using StatisticsAnalysisTool.Network.Manager;
using StatisticsAnalysisTool.Notification;
using StatisticsAnalysisTool.Updater;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace StatisticsAnalysisTool.Linux.Startup;

public sealed class AppStartup
{
    private static readonly Stopwatch ApplicationUptimeStopwatch = Stopwatch.StartNew();

    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly Func<ServerType, Action<double, string>, double, double, Task<bool>> _initializeGameData;
    private readonly Action<string, string> _showInfoDialog;
    private readonly bool _installProcessHooks;
    private MainWindowViewModel _mainWindowViewModel;
    private TrackingController _trackingController;
    private ServerUserDataCoordinator _serverUserDataCoordinator;
    private bool _isEarlyShutdown;
    private bool _isCleanedUp;

    /// <param name="initializeGameData">Defaults to <see cref="GameData.InitializeMainGameDataFilesAsync"/>; tests inject a fake.</param>
    /// <param name="showInfoDialog">Defaults to a modal message box; tests inject a recorder.</param>
    /// <param name="installProcessHooks">False in tests: the exception hooks and logger are process wide.</param>
    internal AppStartup(
        IClassicDesktopStyleApplicationLifetime desktop,
        Func<ServerType, Action<double, string>, double, double, Task<bool>> initializeGameData = null,
        Action<string, string> showInfoDialog = null,
        bool installProcessHooks = true)
    {
        _desktop = desktop;
        _initializeGameData = initializeGameData ?? GameData.InitializeMainGameDataFilesAsync;
        _showInfoDialog = showInfoDialog ?? ((title, text) =>
            System.Windows.MessageBox.Show(text, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information));
        _installProcessHooks = installProcessHooks;
    }

    public static TimeSpan ApplicationUptime => ApplicationUptimeStopwatch.Elapsed;

    public bool IsEarlyShutdown => _isEarlyShutdown;

    public MainWindow MainWindow { get; private set; }

    /// <summary>Runs the whole startup. Never throws: failures take the same fatal path as the WPF catch block.</summary>
    public async Task RunAsync()
    {
        try
        {
            _desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _desktop.ShutdownRequested += OnShutdownRequested;
            _desktop.Exit += OnExit;

            AppDataPaths.EnsureBaseDirectory();
            var migrationMessages = AppDataMigration.MigrateLegacyRuntimeData();
            AppDataPaths.EnsureRuntimeDirectories();
            if (_installProcessHooks)
            {
                InitLogger();
            }

            AppDataMigration.LogMessages(migrationMessages);
            Log.Information("Tool started with v{Version}", Assembly.GetExecutingAssembly().GetName().Version);

            SystemInfo.LogSystemInfo();
            DebugConsole.UseEnums(typeof(EventCodes), typeof(OperationCodes));

            if (_installProcessHooks)
            {
                GlobalExceptionHandlers.Install();
            }

            await SettingsController.LoadSettingsAsync();
            _ = WindowsStartupService.TrySetEnabled(SettingsController.CurrentSettings.IsStartWithWindowsActive);

            if (SettingsController.CurrentSettings.IsOpenDebugConsoleWhenStartingTheToolChecked)
            {
                DebugConsole.Attach("SAT Debug Console");
                DebugConsole.Configure(SettingsController.CurrentSettings.DebugConsoleFilter);
            }

            Culture.SetCulture(Culture.GetCultureByIetfLanguageTag(SettingsController.CurrentSettings.CurrentCultureIetfLanguageTag));
            if (!LocalizationController.Init())
            {
                EarlyShutdown();
                return;
            }

            if (!ShowFirstStartGuideIfNeeded())
            {
                EarlyShutdown();
                return;
            }

            var toolLoadingWindowViewModel = new ToolLoadingWindowViewModel();
            var toolLoadingWindow = new ToolLoadingWindow(toolLoadingWindowViewModel);
            var isToolLoadingWindowShown = false;

            void ReportStartupProgress(double progress, string currentTaskName)
            {
                void UpdateProgress()
                {
                    if (!SettingsController.CurrentSettings.IsStartInSystemTrayActive
                        && !isToolLoadingWindowShown)
                    {
                        toolLoadingWindow.Show();
                        isToolLoadingWindowShown = true;
                    }

                    toolLoadingWindowViewModel.UpdateProgress(progress, currentTaskName);
                }

                if (Dispatcher.UIThread.CheckAccess())
                {
                    UpdateProgress();
                    return;
                }

                Dispatcher.UIThread.Invoke(UpdateProgress);
            }

            try
            {
                if (!await _initializeGameData(SettingsController.CurrentSettings.ServerType, ReportStartupProgress, 0, 40))
                {
                    EarlyShutdown();
                    return;
                }

                ShowCaptureInfoDialogOnFirstStart();

                ReportStartupProgress(40, "Backups");
                await BackupController.DeleteOldestBackupsIfNeededAsync();

                _desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                AppDataPaths.SetActiveUserDataServer(SettingsController.CurrentSettings.StartupUserDataServerLocation);

                RegisterServicesEarly();
                MainWindow = new MainWindow(_mainWindowViewModel);
                _desktop.MainWindow = MainWindow;
                RegisterServicesLate();

                await _mainWindowViewModel.InitMainWindowDataAsync(ReportStartupProgress, 45, 95);
                ReportStartupProgress(95, LocalizationController.Translation("SERVER"));
                await _serverUserDataCoordinator.SyncCurrentServerAsync();
                ReportStartupProgress(97, LocalizationController.Translation("TRACKING"));
                await _trackingController.InitTrackingAsync();
                ReportStartupProgress(100, LocalizationController.Translation("TRACKING"));
                MainWindow.Show();
            }
            finally
            {
                toolLoadingWindow.Close();
            }

            await AutoUpdateController.StartBackgroundUpdateLoopAsync();

            Utilities.AnotherAppToStart(SettingsController.CurrentSettings.AnotherAppToStartPath);
        }
        catch (Exception ex)
        {
            _isEarlyShutdown = true;
            try
            {
                Log.Fatal(ex, "An unexpected fatal error has occurred.");
                System.Windows.MessageBox.Show("An unexpected error has occurred.", "Statistics Analysis Tool",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }

            _desktop.Shutdown();
        }
    }

    private void EarlyShutdown()
    {
        _isEarlyShutdown = true;
        _desktop.Shutdown();
    }

    private void ShowCaptureInfoDialogOnFirstStart()
    {
        var environment = CaptureEnvironment.Current;
        if (!environment.IsCapable)
        {
            Log.Warning("{Guidance}", CaptureGuidance.PermissionMessage(environment.ProcessPath, environment.EntryAssemblyPath));
        }

        if (!SettingsController.CurrentSettings.IsNpcapInfoDialogShownOnStart)
        {
            return;
        }

        var (title, text) = CaptureStartupDialog.Build(
            LocalizationController.Translation("NPCAP_INFO_DIALOG_TITLE"),
            LocalizationController.Translation("NPCAP_INFO_DIALOG_MESSAGE"),
            LocalizationController.Translation("NPCAP_INFO_DIALOG_LINK_TEXT"),
            environment);
        _showInfoDialog(title, text);

        SettingsController.CurrentSettings.IsNpcapInfoDialogShownOnStart = false;
    }

    private static bool ShowFirstStartGuideIfNeeded()
    {
        if (SettingsController.CurrentSettings.HasCompletedFirstStartGuide)
        {
            return true;
        }

        Log.Information("First start guide opened.");
        var dialog = new FirstStartGuideWindow();
        var result = dialog.ShowDialog();

        if (result is true)
        {
            Log.Information("First start guide completed.");
            return true;
        }

        Log.Information("First start guide closed before completion.");
        return false;
    }

    private static void InitLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(
                AppDataPaths.LogFilePattern,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                restrictedToMinimumLevel: LogEventLevel.Information)
            .MinimumLevel.Verbose()
            .WriteTo.Debug(
                restrictedToMinimumLevel: LogEventLevel.Verbose)
            .CreateLogger();
    }

    private void RegisterServicesEarly()
    {
        ServiceLocator.Register<AlbionServerDetectionService>(new AlbionServerDetectionService());

        _mainWindowViewModel = new MainWindowViewModel();
        ServiceLocator.Register<MainWindowViewModel>(_mainWindowViewModel);

        // The Linux notification manager needs no dispatcher argument (it uses Avalonia's UI thread).
        ServiceLocator.Register<SatNotificationManager>(new SatNotificationManager());
    }

    private void RegisterServicesLate()
    {
        _trackingController = new TrackingController(_mainWindowViewModel);
        ServiceLocator.Register<TrackingController>(_trackingController);

        _serverUserDataCoordinator = new ServerUserDataCoordinator(
            ServiceLocator.Resolve<AlbionServerDetectionService>(),
            _mainWindowViewModel,
            _trackingController);
        ServiceLocator.Register<ServerUserDataCoordinator>(_serverUserDataCoordinator);
    }

    // WPF SessionEnding: the desktop session asks the app to quit (logout, shutdown).
    private void OnShutdownRequested(object sender, ShutdownRequestedEventArgs e) => Cleanup();

    // WPF OnExit.
    private void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e) => Cleanup();

    /// <summary>The body shared by OnExit and OnSessionEnding upstream; both can fire for one shutdown, so it runs once.</summary>
    internal void Cleanup()
    {
        if (_isCleanedUp || _isEarlyShutdown || _trackingController is null)
        {
            return;
        }

        _isCleanedUp = true;
        try
        {
            ServiceLocator.Resolve<SatNotificationManager>().StopShowingNotifications();
            AutoUpdateController.Dispose();
            _trackingController.PrepareForShutdown();
            CriticalData.Save();

            if (!BackupController.ExistBackupOnSettingConditions())
            {
                BackupController.Save();
            }
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
