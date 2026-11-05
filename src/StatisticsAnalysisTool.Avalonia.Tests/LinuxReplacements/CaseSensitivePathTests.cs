using FluentAssertions;
using NUnit.Framework;
using StatisticAnalysisTool.Extractor.Enums;
using StatisticsAnalysisTool.Common;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

[NonParallelizable]
public class CaseSensitivePathTests
{
    private string _root;

    [SetUp]
    public void SetUp() => _root = Directory.CreateTempSubdirectory("sat-paths-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_root, true);

    [Test]
    public void GameFile_ResolvesAnExistingFileIgnoringCase()
    {
        using var _ = AppDataPaths.UseRuntimeBaseDirectoryForTests(_root);
        Directory.CreateDirectory(AppDataPaths.GameFilesDirectory);
        var written = Path.Combine(AppDataPaths.GameFilesDirectory, "indexedItems.json");
        File.WriteAllText(written, "[]");

        AppDataPaths.GameFile("IndexedItems.json").Should().Be(written);
    }

    [Test]
    public void GameFile_PrefersTheExactNameAndKeepsTheRequestedNameForNewFiles()
    {
        using var _ = AppDataPaths.UseRuntimeBaseDirectoryForTests(_root);
        Directory.CreateDirectory(AppDataPaths.GameFilesDirectory);
        File.WriteAllText(Path.Combine(AppDataPaths.GameFilesDirectory, "Items.json"), "[]");
        File.WriteAllText(Path.Combine(AppDataPaths.GameFilesDirectory, "items.json"), "[]");

        AppDataPaths.GameFile("Items.json").Should().Be(Path.Combine(AppDataPaths.GameFilesDirectory, "Items.json"));
        AppDataPaths.GameFile("new.json").Should().Be(Path.Combine(AppDataPaths.GameFilesDirectory, "new.json"));
    }

    [Test]
    public void GameFile_ChoosesDeterministicallyWhenOnlyCaseDifferentMatchesExist()
    {
        using var _ = AppDataPaths.UseRuntimeBaseDirectoryForTests(_root);
        Directory.CreateDirectory(AppDataPaths.GameFilesDirectory);
        foreach (var name in new[] { "items.JSON", "ITEMS.json", "Items.json" })
        {
            File.WriteAllText(Path.Combine(AppDataPaths.GameFilesDirectory, name), "[]");
        }

        // ordinal order: "ITEMS.json" < "Items.json" < "items.JSON"
        for (var i = 0; i < 3; i++)
        {
            AppDataPaths.GameFile("iTEMS.json").Should().Be(Path.Combine(AppDataPaths.GameFilesDirectory, "ITEMS.json"));
        }
    }

    [Test]
    public void Extractor_FindsTheBinFilesUnderTheGameFolder()
    {
        var gameData = Path.Combine(_root, "game", "Albion-Online_Data", "StreamingAssets", "GameData");
        Directory.CreateDirectory(Path.Combine(gameData, "cluster"));
        foreach (var name in new[] { "items", "mobs", "spells", "mists", "cluster/world" })
        {
            File.WriteAllText(Path.Combine(gameData, name + ".bin"), "x");
        }

        StatisticAnalysisTool.Extractor.Extractor.IsValidMainGameFolder(_root, ServerType.Live).Should().BeTrue();
    }

    [Test]
    public void Extractor_AcceptsBackslashSeparatedBinNames()
    {
        var gameData = Path.Combine(_root, "game", "Albion-Online_Data", "StreamingAssets", "GameData");
        Directory.CreateDirectory(Path.Combine(gameData, "cluster"));
        File.WriteAllText(Path.Combine(gameData, "cluster", "world.bin"), "x");

        StatisticAnalysisTool.Extractor.Extractor.IsBinFileNewer(Path.Combine(_root, "world-modified.json"), _root, ServerType.Live, "cluster\\world")
            .Should().BeTrue();
    }
}
