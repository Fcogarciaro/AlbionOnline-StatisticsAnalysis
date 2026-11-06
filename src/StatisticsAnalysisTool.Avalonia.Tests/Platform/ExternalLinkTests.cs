using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.Platform;

public class ExternalLinkTests
{
    [TestCase("https://github.com/Triky313/AlbionOnline-StatisticsAnalysis")]
    [TestCase("http://example.org/a?b=c")]
    public void IsOpenable_AcceptsWebLinks(string url) => ExternalLink.IsOpenable(url).Should().BeTrue();

    [TestCase(null)]
    [TestCase("")]
    [TestCase("file:///etc/passwd")]
    [TestCase("javascript:alert(1)")]
    [TestCase("/usr/bin/xterm")]
    [TestCase("--help")]
    [TestCase("ftp://example.org/x")]
    public void IsOpenable_RejectsEverythingElse(string url) => ExternalLink.IsOpenable(url).Should().BeFalse();

    [Test]
    public void CreateStartInfo_PassesTheUrlAsASingleArgumentWithoutAShell()
    {
        var info = ExternalLink.CreateStartInfo("https://example.org/a b;rm -rf");

        info.FileName.Should().Be("xdg-open");
        info.UseShellExecute.Should().BeFalse();
        info.ArgumentList.Should().ContainSingle().Which.Should().Be("https://example.org/a%20b;rm%20-rf");
    }
}
