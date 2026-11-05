using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class AppDataPathsTests
{
    private IDisposable _xdg;
    private string _instance;

    [SetUp]
    public void SetUp()
    {
        _xdg = XdgDirectories.UseForTests(XdgDirectories.Resolve(name => name switch
        {
            "XDG_CONFIG_HOME" => "/cfg",
            "XDG_DATA_HOME" => "/data",
            "XDG_CACHE_HOME" => "/cache",
            _ => "/home/u"
        }));
        _instance = AppInstance.InstanceId;
    }

    [TearDown]
    public void TearDown() => _xdg.Dispose();

    [Test]
    public void SettingsFile_LivesInTheConfigDirectory()
    {
        AppDataPaths.SettingsFile.Should().Be($"/cfg/StatisticsAnalysisTool/Instances/{_instance}/Settings.json");
    }

    [Test]
    public void UserDataBackupsLogsAndGameFiles_LiveInTheDataDirectory()
    {
        var root = $"/data/StatisticsAnalysisTool/Instances/{_instance}";
        AppDataPaths.Backups.Should().Be($"{root}/Backups");
        AppDataPaths.Logs.Should().Be($"{root}/logs");
        AppDataPaths.GameFiles.Should().Be($"{root}/GameFiles");
        AppDataPaths.GetUserDataDirectory(ServerLocation.Europe).Should().Be($"{root}/UserData-EUROPE");
        AppDataPaths.LegacyDefaultDirectory.Should().Be("/data/StatisticsAnalysisTool/Default");
    }

    [Test]
    public void TempAndImageCaches_LiveInTheCacheDirectory()
    {
        var root = $"/cache/StatisticsAnalysisTool/Instances/{_instance}";
        AppDataPaths.Temp.Should().Be($"{root}/temp");
        AppDataPaths.ImageResources.Should().Be($"{root}/ImageResources");
        AppDataPaths.SpellImageResources.Should().Be($"{root}/SpellImageResources");
    }

    [Test]
    public void RuntimeBaseDirectoryOverride_KeepsEverythingUnderTheOverride()
    {
        using var _ = AppDataPaths.UseRuntimeBaseDirectoryForTests("/tmp/sat");

        AppDataPaths.SettingsFile.Should().Be("/tmp/sat/Settings.json");
        AppDataPaths.Temp.Should().Be("/tmp/sat/temp");
        AppDataPaths.ImageResources.Should().Be("/tmp/sat/ImageResources");
        AppDataPaths.Backups.Should().Be("/tmp/sat/Backups");
    }
}
