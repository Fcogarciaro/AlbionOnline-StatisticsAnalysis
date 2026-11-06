using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Updater;

namespace StatisticsAnalysisTool.Avalonia.Tests.Updater;

public class LinuxUpdateTests
{
    private const string Asset = "StatisticsAnalysisTool-linux-x64.tar.gz";

    private static ReleaseInfo Release(string tag, bool draft = false, bool prerelease = false, params string[] assets) =>
        new(tag, tag, "notes", $"https://github.com/Fcogarciaro/AlbionOnline-StatisticsAnalysis/releases/tag/{tag}", null, draft, prerelease,
            assets.Select(a => new ReleaseAsset(a, $"https://example.invalid/{tag}/{a}")).ToList());

    [TestCase("linux-v1.2.3", "1.2.3")]
    [TestCase("linux-v1.2.3.4", "1.2.3.4")]
    [TestCase("linux-v1.2", "1.2")]
    [TestCase("LINUX-V2.0.1", "2.0.1")]
    [TestCase("linux-v1.2.3-rc1", "1.2.3")]
    [TestCase("linux-v1.2.3+build5", "1.2.3")]
    public void TryParse_ReadsTheVersionOfALinuxTag(string tag, string expected)
    {
        ReleaseVersion.TryParse(tag, out var version).Should().BeTrue();
        version.Should().Be(Version.Parse(expected));
    }

    [TestCase("v1.2.3")]
    [TestCase("1.2.3")]
    [TestCase("linux-v")]
    [TestCase("linux-vx.y")]
    [TestCase("")]
    [TestCase(null)]
    [TestCase("windows-v1.2.3")]
    public void TryParse_RejectsTagsThatAreNotLinuxReleases(string tag)
    {
        ReleaseVersion.TryParse(tag, out _).Should().BeFalse();
    }

    [TestCase("1.2", "1.2.0", 0)]
    [TestCase("1.2.0.0", "1.2", 0)]
    [TestCase("1.10", "1.9", 1)]
    [TestCase("1.2.3", "1.2.4", -1)]
    public void Compare_TreatsMissingComponentsAsZero(string left, string right, int expected)
    {
        Math.Sign(ReleaseVersion.Compare(Version.Parse(left), Version.Parse(right))).Should().Be(expected);
    }

    [Test]
    public void Select_ReturnsTheNewestReleaseThatCarriesTheTarball()
    {
        var releases = new[]
        {
            Release("linux-v1.1.0", assets: Asset),
            Release("linux-v1.3.0", assets: Asset),
            Release("linux-v1.2.0", assets: Asset)
        };

        var update = UpdateSelector.Select(releases, new Version(1, 0, 0), includePrereleases: false);

        update.Should().NotBeNull();
        update.Release.TagName.Should().Be("linux-v1.3.0");
        update.Version.Should().Be(new Version(1, 3, 0));
    }

    [Test]
    public void Select_ReturnsNothingWhenTheCurrentVersionIsTheNewestOrNewer()
    {
        var releases = new[] { Release("linux-v1.2.0", assets: Asset) };

        UpdateSelector.Select(releases, new Version(1, 2, 0), false).Should().BeNull();
        UpdateSelector.Select(releases, new Version(1, 2, 0, 0), false).Should().BeNull("a missing component equals zero");
        UpdateSelector.Select(releases, new Version(2, 0, 0), false).Should().BeNull();
    }

    [Test]
    public void Select_SkipsDraftsPrereleasesNonLinuxTagsAndReleasesWithoutTheTarball()
    {
        var releases = new[]
        {
            Release("linux-v9.0.0", draft: true, assets: Asset),
            Release("linux-v8.0.0", prerelease: true, assets: Asset),
            Release("v7.0.0", assets: Asset),
            Release("linux-v6.0.0", assets: "StatisticsAnalysisTool-win-x64.zip"),
            Release("linux-v2.0.0", assets: Asset)
        };

        UpdateSelector.Select(releases, new Version(1, 0, 0), false)!.Release.TagName.Should().Be("linux-v2.0.0");
    }

    [Test]
    public void Select_ConsidersPrereleasesOnlyWhenAsked()
    {
        var releases = new[] { Release("linux-v2.0.0", prerelease: true, assets: Asset), Release("linux-v1.1.0", assets: Asset) };

        UpdateSelector.Select(releases, new Version(1, 0, 0), includePrereleases: false)!.Release.TagName.Should().Be("linux-v1.1.0");
        UpdateSelector.Select(releases, new Version(1, 0, 0), includePrereleases: true)!.Release.TagName.Should().Be("linux-v2.0.0");
    }

    [Test]
    public void Select_ExposesTheTarballAndChecksumAssets()
    {
        var releases = new[] { Release("linux-v1.1.0", assets: [Asset, Asset + ".sha256"]) };

        var update = UpdateSelector.Select(releases, new Version(1, 0, 0), false);

        update.Tarball.Name.Should().Be(Asset);
        update.Checksum.Name.Should().Be(Asset + ".sha256");
    }

    [Test]
    public void Select_ToleratesNullInputs()
    {
        UpdateSelector.Select(null, new Version(1, 0, 0), false).Should().BeNull();
        UpdateSelector.Select([null, Release("linux-v1.1.0", assets: Asset)], new Version(1, 0, 0), false).Should().NotBeNull();
    }

    [Test]
    public void Parse_ReadsTheGitHubReleasesPayload()
    {
        const string json = """
        [
          {
            "tag_name": "linux-v1.2.0", "name": "Linux 1.2.0", "body": "- fixed things", "html_url": "https://github.com/x/y/releases/tag/linux-v1.2.0",
            "draft": false, "prerelease": false, "published_at": "2026-05-01T10:00:00Z",
            "assets": [ { "name": "StatisticsAnalysisTool-linux-x64.tar.gz", "browser_download_url": "https://github.com/x/y/releases/download/linux-v1.2.0/a.tar.gz" } ]
          },
          { "tag_name": "linux-v1.0.0", "draft": true }
        ]
        """;

        var releases = GitHubReleaseParser.Parse(json);

        releases.Should().HaveCount(2);
        releases[0].TagName.Should().Be("linux-v1.2.0");
        releases[0].Body.Should().Be("- fixed things");
        releases[0].PublishedAt.Should().Be(new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero));
        releases[0].Assets.Should().ContainSingle().Which.DownloadUrl.Should().EndWith("a.tar.gz");
        releases[1].IsDraft.Should().BeTrue();
        releases[1].Assets.Should().BeEmpty();
    }

    [TestCase("")]
    [TestCase("not json")]
    [TestCase("{\"message\":\"rate limited\"}")]
    [TestCase("null")]
    public void Parse_ReturnsNothingForUnusablePayloads(string json)
    {
        GitHubReleaseParser.Parse(json).Should().BeEmpty();
    }
}
