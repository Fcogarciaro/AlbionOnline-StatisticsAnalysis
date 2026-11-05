using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Linux.Platform;
using StatisticsAnalysisTool.Properties;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

// Settings.Default (ApplicationSettingsBase) only persists the UpgradeRequired flag at runtime; everything else is
// read-only defaults. On Linux the stock LocalFileSettingsProvider cannot write, so a JSON provider is attached.
// The real settings are JSON as well (SettingsController -> AppDataPaths.SettingsFile).
[NonParallelizable]
public class SettingsPersistenceTests
{
    private string _root;
    private IDisposable _xdg;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "sat-settings-" + Guid.NewGuid().ToString("N"));
        _xdg = XdgDirectories.UseForTests(XdgDirectories.Resolve(name => name.StartsWith("XDG_") ? _root : "/home/u"));
        Settings.Default.Reload();
    }

    [TearDown]
    public void TearDown()
    {
        _xdg.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Test]
    public void UserScopedSetting_RoundTripsThroughSaveAndReload()
    {
        Settings.Default.UpgradeRequired.Should().BeTrue("the declared default is True");

        Settings.Default.UpgradeRequired = false;
        Settings.Default.Save();
        Settings.Default.Reload();

        Settings.Default.UpgradeRequired.Should().BeFalse();
        File.Exists(AppDataPaths.UserSettingsFile).Should().BeTrue();
        AppDataPaths.UserSettingsFile.Should().StartWith(Path.Combine(_root, "StatisticsAnalysisTool"));
    }

    [Test]
    public void ApplicationScopedSettings_KeepTheirDeclaredDefaults()
    {
        Settings.Default.DefaultLanguageCultureName.Should().Be("en-US");
        Settings.Default.IndexedItemsFileName.Should().Be("IndexedItems.json");
    }

    [Test]
    public void Upgrade_DoesNotThrowWithoutAPreviousVersion()
    {
        var act = () => Settings.Default.Upgrade();

        act.Should().NotThrow();
    }

    [Test]
    public void CorruptSettingsFile_FallsBackToDefaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(AppDataPaths.UserSettingsFile)!);
        File.WriteAllText(AppDataPaths.UserSettingsFile, "{ not json");

        Settings.Default.Reload();

        Settings.Default.UpgradeRequired.Should().BeTrue();
    }
}
