// Linux replacement for Updater/AutoUpdateController.cs (and UpdateDownloadSession.cs).
// The WPF implementation is built on NetSparkleUpdater.UI.WPF, Authenticode verification and an installer, none of
// which exist on Linux. Same public surface as the original so callers (FooterViewModel, SettingsControl, App startup)
// compile unchanged.
//
// Design decision (safer option): the Linux updater only NOTIFIES. It reads the GitHub releases of the fork, compares
// the newest `linux-v*` release (that ships StatisticsAnalysisTool-linux-x64.tar.gz) with the running assembly version,
// opens the shared UpdateWindow with the release notes, and its action opens the release page in the browser. It never
// downloads, extracts or swaps files, because: the binary carries file capabilities (setcap) that a replaced file would
// lose silently, the install location may be root-owned (PKGBUILD installs to /opt), and a SHA256 file served from the
// same origin as the archive does not authenticate it. Users update through the package manager or install.sh.
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Serilog;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.Linux.Updater;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;

namespace StatisticsAnalysisTool.Updater;

public static class AutoUpdateController
{
    public const string ReleasesUrl = "https://api.github.com/repos/Fcogarciaro/AlbionOnline-StatisticsAnalysis/releases?per_page=20";

    private static readonly TimeSpan BackgroundInterval = TimeSpan.FromHours(6);
    private static readonly object SyncRoot = new();
    private static readonly HttpClient Http = CreateHttpClient();

    private static UpdateCandidate _pending;
    private static bool _isCheckRunning;
    private static bool _isLoopStarted;
    private static CancellationTokenSource _loopCancellation;

    public static event Action<bool> UpdateAvailabilityChanged;

    public static bool IsUpdateAvailable
    {
        get
        {
            lock (SyncRoot)
            {
                return _pending != null;
            }
        }
    }

    // Seams (tests replace them).
    internal static Func<CancellationToken, Task<string>> FetchReleasesJson { get; set; } = FetchFromGitHubAsync;
    internal static Func<Version> CurrentVersion { get; set; } = () => typeof(AutoUpdateController).Assembly.GetName().Version ?? new Version(0, 0);
    internal static Func<string, bool> OpenUrl { get; set; } = ExternalLink.Open;
    internal static Action<string, string, bool> ShowMessage { get; set; } = ShowMessageBox;
    internal static Func<UpdateWindowViewModel, Func<Task<bool>>, Task> PresentUpdate { get; set; } = PresentInWindowAsync;

    internal static void ResetForTests()
    {
        Dispose();
        lock (SyncRoot)
        {
            _pending = null;
            _isCheckRunning = false;
            _isLoopStarted = false;
        }

        FetchReleasesJson = FetchFromGitHubAsync;
        CurrentVersion = () => typeof(AutoUpdateController).Assembly.GetName().Version ?? new Version(0, 0);
        OpenUrl = ExternalLink.Open;
        ShowMessage = ShowMessageBox;
        PresentUpdate = PresentInWindowAsync;
    }

    public static Task StartBackgroundUpdateLoopAsync()
    {
        lock (SyncRoot)
        {
            if (_isLoopStarted)
            {
                return Task.CompletedTask;
            }

            _isLoopStarted = true;
            _loopCancellation = new CancellationTokenSource();
            _ = RunLoopAsync(_loopCancellation.Token);
        }

        return Task.CompletedTask;
    }

    /// <summary>Manual check (Settings): tells the user the outcome and opens the update window when there is one.</summary>
    public static async Task CheckForUpdatesAsync()
    {
        var outcome = await CheckAsync(CancellationToken.None);
        switch (outcome)
        {
            case CheckOutcome.UpdateAvailable:
                await ShowAvailableUpdateWindowAsync();
                break;
            case CheckOutcome.UpToDate:
                ShowMessage(LocalizationController.Translation("NO_UPDATE_AVAILABLE_MESSAGE"), LocalizationController.Translation("NO_UPDATE_AVAILABLE_TITLE"), false);
                break;
            case CheckOutcome.Failed:
                ShowMessage(LocalizationController.Translation("UPDATE_CHECK_FAILED_MESSAGE"), LocalizationController.Translation("UPDATE_CHECK_FAILED_TITLE"), true);
                break;
        }
    }

    /// <summary>Background check: only flips <see cref="IsUpdateAvailable"/> (the footer shows the badge); failures are logged.</summary>
    internal static Task CheckInBackgroundAsync(CancellationToken cancellationToken = default) => CheckAsync(cancellationToken);

    public static async Task ShowAvailableUpdateWindowAsync()
    {
        UpdateCandidate candidate;
        lock (SyncRoot)
        {
            candidate = _pending;
        }

        if (candidate == null)
        {
            return;
        }

        var viewModel = CreateViewModel(candidate, CurrentVersion());
        await PresentUpdate(viewModel, () => Task.FromResult(OpenUrl(candidate.Release.HtmlUrl)));
    }

    public static void Dispose()
    {
        CancellationTokenSource cancellation;
        lock (SyncRoot)
        {
            cancellation = _loopCancellation;
            _loopCancellation = null;
            _isLoopStarted = false;
        }

        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    private enum CheckOutcome
    {
        UpdateAvailable,
        UpToDate,
        Failed,
        Skipped
    }

    private static async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await CheckAsync(cancellationToken);
            using var timer = new PeriodicTimer(BackgroundInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await CheckAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Log.Error(e, "The background update loop stopped unexpectedly.");
        }
    }

    private static async Task<CheckOutcome> CheckAsync(CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            if (_isCheckRunning)
            {
                return CheckOutcome.Skipped;
            }

            _isCheckRunning = true;
        }

        try
        {
            var json = await FetchReleasesJson(cancellationToken);
            var releases = GitHubReleaseParser.Parse(json);
            var candidate = UpdateSelector.Select(releases, CurrentVersion(), SettingsController.CurrentSettings.IsSuggestPreReleaseUpdatesActive);
            SetPending(candidate);
            return candidate == null ? CheckOutcome.UpToDate : CheckOutcome.UpdateAvailable;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            if (e is TaskCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            Log.Warning(e, "The update check failed.");
            return CheckOutcome.Failed;
        }
        finally
        {
            lock (SyncRoot)
            {
                _isCheckRunning = false;
            }
        }
    }

    private static void SetPending(UpdateCandidate candidate)
    {
        bool? changed = null;
        lock (SyncRoot)
        {
            if ((_pending != null) != (candidate != null))
            {
                changed = candidate != null;
            }

            _pending = candidate;
        }

        if (changed.HasValue)
        {
            UpdateAvailabilityChanged?.Invoke(changed.Value);
        }
    }

    private static UpdateWindowViewModel CreateViewModel(UpdateCandidate candidate, Version current)
    {
        var release = candidate.Release;
        var versionText = candidate.Version.ToString();
        var title = string.IsNullOrWhiteSpace(release.Name) ? $"Statistics Analysis Tool v{versionText}" : release.Name;
        var dateText = release.PublishedAt?.ToString("d", System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
        var items = new ObservableCollection<string>(ReleaseNotes.ToItems(release.Body));
        var groups = new ObservableCollection<UpdateReleaseNoteGroupViewModel>();
        if (items.Count > 0)
        {
            groups.Add(new UpdateReleaseNoteGroupViewModel(title, $"v{versionText}", dateText,
                [new UpdateNoteSectionViewModel(string.Empty, null, items)]));
        }

        return new UpdateWindowViewModel(
            LocalizationController.Translation("UPDATE_AVAILABLE_TITLE"),
            LocalizationController.FormatTranslation("UPDATE_AVAILABLE_HEADLINE", "Statistics Analysis Tool"),
            LocalizationController.FormatTranslation("UPDATE_AVAILABLE_OPTIONAL_MESSAGE", versionText, current.ToString())
            + " Install it with your package manager or install.sh from the release page.",
            title,
            $"v{versionText}",
            dateText,
            groups.Count > 0,
            groups,
            LocalizationController.Translation("UPDATE_REMIND_LATER"),
            "Open release page");
    }

    private static Task PresentInWindowAsync(UpdateWindowViewModel viewModel, Func<Task<bool>> action)
    {
        var window = new UpdateWindow(viewModel, action);
        window.ShowDialog();
        return Task.CompletedTask;
    }

    private static void ShowMessageBox(string message, string title, bool isError) =>
        _ = MessageBox.Show(message, title, MessageBoxButton.OK, isError ? MessageBoxImage.Error : MessageBoxImage.Information);

    private static async Task<string> FetchFromGitHubAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        return await Http.GetStringAsync(ReleasesUrl, timeout.Token);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        var version = typeof(AutoUpdateController).Assembly.GetName().Version?.ToString() ?? "0";
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"StatisticsAnalysisTool-Linux/{version}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }
}

/// <summary>Turns GitHub release markdown into plain lines for the update window.</summary>
internal static class ReleaseNotes
{
    public static string[] ToItems(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        return body.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().TrimStart('#', '-', '*', '+', ' ', '\t'))
            .Where(line => line.Length > 0)
            .ToArray();
    }
}
