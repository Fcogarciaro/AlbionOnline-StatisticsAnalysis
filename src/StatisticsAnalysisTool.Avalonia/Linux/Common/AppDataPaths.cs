using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Common;

// Linux copy of Common/AppDataPaths.cs (keep in sync with upstream). Same members and same sub-structure
// (Instances/<id>/Backups, UserData-*, ...), but split across the XDG roots:
//   config: Settings.json                      -> $XDG_CONFIG_HOME/StatisticsAnalysisTool
//   data:   Backups, UserData-*, logs, GameFiles -> $XDG_DATA_HOME/StatisticsAnalysisTool (same place .NET uses for LocalApplicationData)
//   cache:  temp, ImageResources, SpellImageResources -> $XDG_CACHE_HOME/StatisticsAnalysisTool (re-downloadable)
// With a runtime base directory override (tests), everything lives under the override like upstream.
public static class AppDataPaths
{
    private const string AppDataFolderName = "StatisticsAnalysisTool";
    private const string InstancesDirectoryName = "Instances";
    private const string LegacyDefaultDirectoryName = "Default";
    private const string BackupsDirectoryName = "Backups";
    private const string UserDataDirectoryName = "UserData";
    private const string UserDataAmericaDirectoryName = "UserData-AMERICA";
    private const string UserDataAsiaDirectoryName = "UserData-ASIA";
    private const string UserDataEuropeDirectoryName = "UserData-EUROPE";
    private const string StatisticsDataDirectoryName = "Data";
    private const string DamageMeterSnapshotsDirectoryName = "DamageMeterSnapshots";
    private const string DamageMeterSnapshotsIndexFileName = "index.json";
    private const string TempDirectoryName = "temp";
    private const string SpellImageResourcesDirectoryName = "SpellImageResources";
    private const string LogsDirectoryName = "logs";
    private const string ImageResourcesDirectoryName = "ImageResources";
    private const string GameFilesDirectoryName = "GameFiles";
    private const string SettingsFileName = "Settings.json";
    private const string LogFilePatternName = "sat-.logs";
    private const string SoundDirectoryName = "Sounds";
    private const string LocalizationDirectoryName = "Localization";
    private const string LocalizationFileName = "localization.json";
    private const string ExecutableFileName = "StatisticsAnalysisTool.exe";
    private static string _runtimeBaseDirectoryOverride;
    private static string _installationDirectoryOverride;
    private static string _legacyDefaultDirectoryOverride;
    private static ServerLocation _activeUserDataServerLocation = ServerLocation.Unknown;

    public static string BaseDirectory => XdgDirectories.Current.DataRoot;

    public static string InstancesDirectory => Path.Combine(BaseDirectory, InstancesDirectoryName);

    public static string LegacyDefaultDirectory => _legacyDefaultDirectoryOverride ?? Path.Combine(BaseDirectory, LegacyDefaultDirectoryName);

    public static string Root => Path.Combine(
        InstancesDirectory,
        AppInstance.InstanceId);

    private static string ConfigDirectory => _runtimeBaseDirectoryOverride
        ?? Path.Combine(XdgDirectories.Current.ConfigRoot, InstancesDirectoryName, AppInstance.InstanceId);

    private static string CacheDirectory => _runtimeBaseDirectoryOverride
        ?? Path.Combine(XdgDirectories.Current.CacheRoot, InstancesDirectoryName, AppInstance.InstanceId);

    public static string RuntimeBaseDirectory => _runtimeBaseDirectoryOverride ?? Root;

    public static string InstallationDirectory => _installationDirectoryOverride ?? AppContext.BaseDirectory;

    public static string Backups => Path.Combine(RuntimeBaseDirectory, BackupsDirectoryName);

    public static string LegacyRuntimeUserData => Path.Combine(RuntimeBaseDirectory, UserDataDirectoryName);

    public static string UserData => GetUserDataDirectory(_activeUserDataServerLocation);

    public static string Temp => Path.Combine(CacheDirectory, TempDirectoryName);

    public static string SpellImageResources => Path.Combine(CacheDirectory, SpellImageResourcesDirectoryName);

    public static string Logs => Path.Combine(RuntimeBaseDirectory, LogsDirectoryName);

    public static string ImageResources => Path.Combine(CacheDirectory, ImageResourcesDirectoryName);

    public static string GameFiles => Path.Combine(RuntimeBaseDirectory, GameFilesDirectoryName);

    public static string UserSettingsFile => Path.Combine(ConfigDirectory, "UserSettings.json");

    public static string SettingsFile => Path.Combine(ConfigDirectory, SettingsFileName);

    public static string BackupsDirectory => Backups;

    public static string LegacyRuntimeUserDataDirectory => LegacyRuntimeUserData;

    public static string UserDataDirectory => UserData;

    public static string StatisticsDataDirectory => Path.Combine(UserDataDirectory, StatisticsDataDirectoryName);

    public static string DamageMeterSnapshotsDirectory => Path.Combine(UserDataDirectory, DamageMeterSnapshotsDirectoryName);

    public static string DamageMeterSnapshotsIndexFile => Path.Combine(DamageMeterSnapshotsDirectory, DamageMeterSnapshotsIndexFileName);

    public static ServerLocation ActiveUserDataServerLocation => _activeUserDataServerLocation;

    public static bool IsUserDataAvailable => _activeUserDataServerLocation is ServerLocation.America or ServerLocation.Asia or ServerLocation.Europe;

    public static string TempDirectory => Temp;

    public static string SpellImageResourcesDirectory => SpellImageResources;

    public static string LogsDirectory => Logs;

    public static string ImageResourcesDirectory => ImageResources;

    public static string GameFilesDirectory => GameFiles;

    public static string LogFilePattern => Path.Combine(LogsDirectory, LogFilePatternName);

    public static string SoundDirectory => InstallationFile(SoundDirectoryName);

    public static string SoundFile(string fileName)
    {
        return Path.Combine(SoundDirectory, fileName);
    }

    public static string LocalizationFile => InstallationFile(LocalizationDirectoryName, LocalizationFileName);
    public static string ExecutableFile => InstallationFile(ExecutableFileName);

    public static IReadOnlyCollection<string> RuntimeDirectories =>
    [
        BackupsDirectory,
        TempDirectory,
        SpellImageResourcesDirectory,
        LogsDirectory,
        ImageResourcesDirectory,
        GameFilesDirectory
    ];

    public static IReadOnlyCollection<string> ServerUserDataDirectories =>
    [
        GetUserDataDirectory(ServerLocation.America),
        GetUserDataDirectory(ServerLocation.Asia),
        GetUserDataDirectory(ServerLocation.Europe)
    ];

    public static void SetActiveUserDataServer(ServerLocation serverLocation)
    {
        _activeUserDataServerLocation = serverLocation is ServerLocation.America or ServerLocation.Asia or ServerLocation.Europe
            ? serverLocation
            : ServerLocation.Unknown;
    }

    public static bool TryEnsureUserDataDirectory()
    {
        if (!IsUserDataAvailable)
        {
            return false;
        }

        Directory.CreateDirectory(UserDataDirectory);
        return true;
    }

    public static bool TryEnsureStatisticsDataDirectory()
    {
        if (!TryEnsureUserDataDirectory())
        {
            return false;
        }

        Directory.CreateDirectory(StatisticsDataDirectory);
        return true;
    }

    public static bool TryEnsureDamageMeterSnapshotsDirectory()
    {
        if (!TryEnsureUserDataDirectory())
        {
            return false;
        }

        Directory.CreateDirectory(DamageMeterSnapshotsDirectory);
        return true;
    }

    public static string GetUserDataDirectory(ServerLocation serverLocation)
    {
        return serverLocation switch
        {
            ServerLocation.America => Path.Combine(RuntimeBaseDirectory, UserDataAmericaDirectoryName),
            ServerLocation.Asia => Path.Combine(RuntimeBaseDirectory, UserDataAsiaDirectoryName),
            ServerLocation.Europe => Path.Combine(RuntimeBaseDirectory, UserDataEuropeDirectoryName),
            _ => LegacyRuntimeUserDataDirectory
        };
    }

    public static string UserDataFile(string fileName)
    {
        return Path.Combine(UserDataDirectory, fileName);
    }

    public static bool IsDisabledUserDataPath(string path)
    {
        if (IsUserDataAvailable || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        var legacyUserDataPath = Path.GetFullPath(LegacyRuntimeUserDataDirectory);
        return fullPath.StartsWith(legacyUserDataPath, StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryGetUserDataServerLocation(string path, out ServerLocation serverLocation)
    {
        serverLocation = ServerLocation.Unknown;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        foreach (var knownServerLocation in new[] { ServerLocation.America, ServerLocation.Asia, ServerLocation.Europe })
        {
            var userDataPath = Path.GetFullPath(GetUserDataDirectory(knownServerLocation));
            var userDataPathWithSeparator = Path.TrimEndingDirectorySeparator(userDataPath) + Path.DirectorySeparatorChar;
            if (fullPath.Equals(userDataPath, StringComparison.OrdinalIgnoreCase)
                || fullPath.StartsWith(userDataPathWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                serverLocation = knownServerLocation;
                return true;
            }
        }

        return false;
    }

    // Linux file systems are case-sensitive: upstream asks for "IndexedItems.json" (Settings default) while the
    // extractor writes "indexedItems.json". An exact match wins, then any case-insensitive match, then the requested name.
    public static string GameFile(string fileName)
    {
        var exact = Path.Combine(GameFilesDirectory, fileName);
        if (File.Exists(exact) || !Directory.Exists(GameFilesDirectory))
        {
            return exact;
        }

        var match = Directory.EnumerateFiles(GameFilesDirectory)
            .FirstOrDefault(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase));
        return match ?? exact;
    }

    public static string ItemImageFile(string fileName)
    {
        return Path.Combine(ImageResourcesDirectory, fileName);
    }

    public static string SpellImageFile(string fileName)
    {
        return Path.Combine(SpellImageResourcesDirectory, fileName);
    }

    public static string TempFile(string fileName)
    {
        return Path.Combine(TempDirectory, fileName);
    }

    public static string InstallationFile(params string[] pathParts)
    {
        return Path.Combine([InstallationDirectory, .. pathParts]);
    }

    public static string LegacyBackupsDirectory => Path.Combine(InstallationDirectory, BackupsDirectoryName);

    public static string LegacyUserDataDirectory => Path.Combine(InstallationDirectory, UserDataDirectoryName);

    public static string LegacyTempDirectory => Path.Combine(InstallationDirectory, TempDirectoryName);

    public static string LegacySpellImageResourcesDirectory => Path.Combine(InstallationDirectory, SpellImageResourcesDirectoryName);

    public static string LegacyLogsDirectory => Path.Combine(InstallationDirectory, LogsDirectoryName);

    public static string LegacyImageResourcesDirectory => Path.Combine(InstallationDirectory, ImageResourcesDirectoryName);

    public static string LegacyGameFilesDirectory => Path.Combine(InstallationDirectory, GameFilesDirectoryName);

    public static string LegacySettingsFile => Path.Combine(InstallationDirectory, SettingsFileName);

    public static string LegacyDefaultBackupsDirectory => Path.Combine(LegacyDefaultDirectory, BackupsDirectoryName);

    public static string LegacyDefaultUserDataDirectory => Path.Combine(LegacyDefaultDirectory, UserDataDirectoryName);

    public static string LegacyDefaultTempDirectory => Path.Combine(LegacyDefaultDirectory, TempDirectoryName);

    public static string LegacyDefaultSpellImageResourcesDirectory => Path.Combine(LegacyDefaultDirectory, SpellImageResourcesDirectoryName);

    public static string LegacyDefaultLogsDirectory => Path.Combine(LegacyDefaultDirectory, LogsDirectoryName);

    public static string LegacyDefaultImageResourcesDirectory => Path.Combine(LegacyDefaultDirectory, ImageResourcesDirectoryName);

    public static string LegacyDefaultGameFilesDirectory => Path.Combine(LegacyDefaultDirectory, GameFilesDirectoryName);

    public static string LegacyDefaultSettingsFile => Path.Combine(LegacyDefaultDirectory, SettingsFileName);

    internal static IDisposable UseRuntimeBaseDirectoryForTests(string runtimeBaseDirectory)
    {
        var previousRuntimeBaseDirectoryOverride = _runtimeBaseDirectoryOverride;
        var previousActiveUserDataServerLocation = _activeUserDataServerLocation;
        _runtimeBaseDirectoryOverride = runtimeBaseDirectory;
        _activeUserDataServerLocation = ServerLocation.Unknown;
        return new RuntimeBaseDirectoryOverrideScope(previousRuntimeBaseDirectoryOverride, previousActiveUserDataServerLocation);
    }

    internal static IDisposable UsePathOverridesForTests(
        string runtimeBaseDirectory,
        string installationDirectory,
        string legacyDefaultDirectory)
    {
        var previousRuntimeBaseDirectoryOverride = _runtimeBaseDirectoryOverride;
        var previousInstallationDirectoryOverride = _installationDirectoryOverride;
        var previousLegacyDefaultDirectoryOverride = _legacyDefaultDirectoryOverride;
        var previousActiveUserDataServerLocation = _activeUserDataServerLocation;

        _runtimeBaseDirectoryOverride = runtimeBaseDirectory;
        _installationDirectoryOverride = installationDirectory;
        _legacyDefaultDirectoryOverride = legacyDefaultDirectory;
        _activeUserDataServerLocation = ServerLocation.Unknown;

        return new PathOverrideScope(
            previousRuntimeBaseDirectoryOverride,
            previousInstallationDirectoryOverride,
            previousLegacyDefaultDirectoryOverride,
            previousActiveUserDataServerLocation);
    }

    public static void EnsureBaseDirectory()
    {
        Directory.CreateDirectory(RuntimeBaseDirectory);
        Directory.CreateDirectory(ConfigDirectory);
    }

    public static void EnsureRuntimeDirectories()
    {
        EnsureBaseDirectory();

        foreach (var runtimeDirectory in RuntimeDirectories)
        {
            Directory.CreateDirectory(runtimeDirectory);
        }
    }

    private sealed class RuntimeBaseDirectoryOverrideScope : IDisposable
    {
        private readonly string _previousRuntimeBaseDirectoryOverride;
        private readonly ServerLocation _previousActiveUserDataServerLocation;
        private bool _isDisposed;

        public RuntimeBaseDirectoryOverrideScope(string previousRuntimeBaseDirectoryOverride, ServerLocation previousActiveUserDataServerLocation)
        {
            _previousRuntimeBaseDirectoryOverride = previousRuntimeBaseDirectoryOverride;
            _previousActiveUserDataServerLocation = previousActiveUserDataServerLocation;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _runtimeBaseDirectoryOverride = _previousRuntimeBaseDirectoryOverride;
            _activeUserDataServerLocation = _previousActiveUserDataServerLocation;
            _isDisposed = true;
        }
    }

    private sealed class PathOverrideScope : IDisposable
    {
        private readonly string _previousRuntimeBaseDirectoryOverride;
        private readonly string _previousInstallationDirectoryOverride;
        private readonly string _previousLegacyDefaultDirectoryOverride;
        private readonly ServerLocation _previousActiveUserDataServerLocation;
        private bool _isDisposed;

        public PathOverrideScope(
            string previousRuntimeBaseDirectoryOverride,
            string previousInstallationDirectoryOverride,
            string previousLegacyDefaultDirectoryOverride,
            ServerLocation previousActiveUserDataServerLocation)
        {
            _previousRuntimeBaseDirectoryOverride = previousRuntimeBaseDirectoryOverride;
            _previousInstallationDirectoryOverride = previousInstallationDirectoryOverride;
            _previousLegacyDefaultDirectoryOverride = previousLegacyDefaultDirectoryOverride;
            _previousActiveUserDataServerLocation = previousActiveUserDataServerLocation;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _runtimeBaseDirectoryOverride = _previousRuntimeBaseDirectoryOverride;
            _installationDirectoryOverride = _previousInstallationDirectoryOverride;
            _legacyDefaultDirectoryOverride = _previousLegacyDefaultDirectoryOverride;
            _activeUserDataServerLocation = _previousActiveUserDataServerLocation;
            _isDisposed = true;
        }
    }
}
