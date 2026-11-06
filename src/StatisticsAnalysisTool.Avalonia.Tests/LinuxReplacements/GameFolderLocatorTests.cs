using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class GameFolderLocatorTests
{
    private string _home;

    [SetUp]
    public void SetUp() => _home = Directory.CreateTempSubdirectory("sat-home").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_home, true);

    private string Make(params string[] segments)
    {
        var path = Path.Combine(new[] { _home }.Concat(segments).ToArray());
        Directory.CreateDirectory(path);
        return path;
    }

    private static Func<string, string> Env(params (string, string)[] values) => name => values.FirstOrDefault(v => v.Item1 == name).Item2;

    [Test]
    public void Candidates_ListSteamHomesAndWinePrefixesInDocumentedOrder()
    {
        var candidates = GameFolderLocator.Candidates(_home, Env()).ToList();

        candidates.Should().ContainInOrder(
        [
            Path.Combine(_home, ".local/share/Steam/steamapps/common/Albion Online"),
            Path.Combine(_home, ".steam/steam/steamapps/common/Albion Online"),
            Path.Combine(_home, ".var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/Albion Online")
        ]);
        candidates.Should().Contain(Path.Combine(_home, ".wine/drive_c/Program Files (x86)/AlbionOnline"));
        candidates.Should().Contain(Path.Combine(_home, ".wine/drive_c/Program Files/AlbionOnline"));
        candidates.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void Candidates_IncludeTheNativeLinuxClientInHome()
    {
        var candidates = GameFolderLocator.Candidates(_home, Env()).ToList();

        candidates.Should().Contain(Path.Combine(_home, "albiononline"));
    }

    [Test]
    public void Candidates_HonorXdgDataHomeAndWinePrefix()
    {
        var data = Path.Combine(_home, "xdgdata");
        var prefix = Path.Combine(_home, "myprefix");

        var candidates = GameFolderLocator.Candidates(_home, Env(("XDG_DATA_HOME", data), ("WINEPREFIX", prefix))).ToList();

        candidates.Should().Contain(Path.Combine(data, "Steam/steamapps/common/Albion Online"));
        candidates.Should().Contain(Path.Combine(prefix, "drive_c/Program Files (x86)/AlbionOnline"));
    }

    [Test]
    public void Candidates_IgnoreARelativeXdgDataHome()
    {
        var candidates = GameFolderLocator.Candidates(_home, Env(("XDG_DATA_HOME", "relative/dir"))).ToList();

        candidates.Should().NotContain(c => c.StartsWith("relative"));
    }

    [Test]
    public void Candidates_IncludeSteamLibrariesFromLibraryFoldersVdf()
    {
        var steamApps = Make(".local", "share", "Steam", "steamapps");
        File.WriteAllText(Path.Combine(steamApps, "libraryfolders.vdf"), """
            "libraryfolders"
            {
                "0"
                {
                    "path"		"/home/u/.local/share/Steam"
                }
                "1"
                {
                    "path"		"/mnt/games/SteamLibrary"
                    "label"		""
                }
            }
            """);

        var candidates = GameFolderLocator.Candidates(_home, Env()).ToList();

        candidates.Should().Contain("/mnt/games/SteamLibrary/steamapps/common/Albion Online");
    }

    [Test]
    public void Candidates_IncludeEveryWinePrefixBelowGamesAndBottles()
    {
        Make("Games", "albion-online", "drive_c");
        Make(".local", "share", "bottles", "bottles", "Albion", "drive_c");
        Make("Games", "not-a-prefix");

        var candidates = GameFolderLocator.Candidates(_home, Env()).ToList();

        candidates.Should().Contain(Path.Combine(_home, "Games/albion-online/drive_c/Program Files (x86)/AlbionOnline"));
        candidates.Should().Contain(Path.Combine(_home, ".local/share/bottles/bottles/Albion/drive_c/Program Files (x86)/AlbionOnline"));
        candidates.Should().NotContain(c => c.Contains("not-a-prefix"));
    }

    [Test]
    public void FindValid_ReturnsOnlyExistingFoldersThatPassTheValidation()
    {
        var steam = Make(".local", "share", "Steam", "steamapps", "common", "Albion Online");
        var wine = Make(".wine", "drive_c", "Program Files (x86)", "AlbionOnline");

        var found = GameFolderLocator.FindValid(_home, Env(), folder => folder == steam).ToList();

        found.Should().Equal(steam);
        GameFolderLocator.FindValid(_home, Env(), _ => true).Should().Equal(steam, wine);
        GameFolderLocator.FindValid(_home, Env(), _ => false).Should().BeEmpty();
    }

    [Test]
    public void FindValid_SurvivesAValidationThatThrows()
    {
        Make(".local", "share", "Steam", "steamapps", "common", "Albion Online");

        var found = GameFolderLocator.FindValid(_home, Env(), _ => throw new UnauthorizedAccessException()).ToList();

        found.Should().BeEmpty();
    }

    [Test]
    public void SuggestedStart_IsTheFirstExistingCandidateOrAnExistingParent()
    {
        GameFolderLocator.SuggestedStart(_home, Env()).Should().Be(_home, "nothing exists below the home folder");
        var steamApps = Make(".local", "share", "Steam", "steamapps");

        GameFolderLocator.SuggestedStart(_home, Env()).Should().Be(steamApps, "the deepest existing parent of the Steam candidate");
    }
}
