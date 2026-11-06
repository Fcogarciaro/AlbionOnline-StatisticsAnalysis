using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class XdgDirectoriesTests
{
    private static Func<string, string> Env(params (string, string)[] values) => name => values.FirstOrDefault(v => v.Item1 == name).Item2;

    [Test]
    public void Resolve_HonorsAbsoluteXdgVariables()
    {
        var xdg = XdgDirectories.Resolve(Env(
            ("XDG_CONFIG_HOME", "/cfg"), ("XDG_DATA_HOME", "/data"), ("XDG_CACHE_HOME", "/cache"), ("HOME", "/home/u")));

        xdg.ConfigRoot.Should().Be("/cfg/StatisticsAnalysisTool");
        xdg.DataRoot.Should().Be("/data/StatisticsAnalysisTool");
        xdg.CacheRoot.Should().Be("/cache/StatisticsAnalysisTool");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("relative/dir")]
    public void Resolve_FallsBackToHomeDefaults_ForEmptyOrRelativeValues(string value)
    {
        var xdg = XdgDirectories.Resolve(Env(
            ("XDG_CONFIG_HOME", value), ("XDG_DATA_HOME", value), ("XDG_CACHE_HOME", value), ("HOME", "/home/u")));

        xdg.ConfigRoot.Should().Be("/home/u/.config/StatisticsAnalysisTool");
        xdg.DataRoot.Should().Be("/home/u/.local/share/StatisticsAnalysisTool");
        xdg.CacheRoot.Should().Be("/home/u/.cache/StatisticsAnalysisTool");
    }
}
