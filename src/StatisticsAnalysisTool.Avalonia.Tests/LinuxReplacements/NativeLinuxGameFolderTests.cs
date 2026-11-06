using System;
using System.IO;
using NUnit.Framework;
using StatisticAnalysisTool.Extractor.Enums;
using ExtractorType = StatisticAnalysisTool.Extractor.Extractor;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

// The native Linux client installs the server folders as "game_x64" (and "<server>_x64") instead of "game".
[TestFixture]
public class NativeLinuxGameFolderTests
{
    private static readonly string[] RequiredBins = ["items.bin", "mobs.bin", "spells.bin", "mists.bin", Path.Combine("cluster", "world.bin")];

    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "sat-native-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_root, true);
    }

    private void CreateServerFolder(string serverFolder)
    {
        var gameData = Path.Combine(_root, serverFolder, "Albion-Online_Data", "StreamingAssets", "GameData");
        foreach (var bin in RequiredBins)
        {
            var file = Path.Combine(gameData, bin);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllBytes(file, [0]);
        }
    }

    [Test]
    public void NativeLinuxLayoutIsValid()
    {
        CreateServerFolder("game_x64");

        Assert.That(ExtractorType.IsValidMainGameFolder(_root, ServerType.Live), Is.True);
    }

    [Test]
    public void NativeLinuxStagingLayoutIsValid()
    {
        CreateServerFolder("staging_x64");

        Assert.That(ExtractorType.IsValidMainGameFolder(_root, ServerType.Staging), Is.True);
    }

    [Test]
    public void WindowsLayoutStaysValid()
    {
        CreateServerFolder("game");

        Assert.That(ExtractorType.IsValidMainGameFolder(_root, ServerType.Live), Is.True);
    }

    [Test]
    public void EmptyFolderIsInvalid()
    {
        Assert.That(ExtractorType.IsValidMainGameFolder(_root, ServerType.Live), Is.False);
    }

    [Test]
    public void IsBinFileNewerReadsTheNativeLinuxLayout()
    {
        CreateServerFolder("game_x64");
        var toolFile = Path.Combine(_root, "items.json");
        File.WriteAllText(toolFile, "{}");
        File.SetLastWriteTimeUtc(toolFile, DateTime.UtcNow.AddDays(-1));

        Assert.That(ExtractorType.IsBinFileNewer(toolFile, _root, ServerType.Live, "items"), Is.True);
    }
}
