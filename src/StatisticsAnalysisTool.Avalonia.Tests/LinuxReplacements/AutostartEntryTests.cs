using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class AutostartEntryTests
{
    private static Func<string, string> Env(params (string, string)[] values) => name => values.FirstOrDefault(v => v.Item1 == name).Item2;

    [Test]
    public void ResolveDirectory_HonorsAbsoluteXdgConfigHome()
    {
        AutostartEntry.ResolveDirectory(Env(("XDG_CONFIG_HOME", "/cfg"), ("HOME", "/home/u"))).Should().Be("/cfg/autostart");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("relative/path")]
    public void ResolveDirectory_IgnoresEmptyOrRelativeXdgConfigHome(string xdg)
    {
        AutostartEntry.ResolveDirectory(Env(("XDG_CONFIG_HOME", xdg), ("HOME", "/home/u"))).Should().Be("/home/u/.config/autostart");
    }

    [Test]
    public void BuildExec_UsesTheExecutable_OrDotnetHostWithTheDll()
    {
        AutostartEntry.BuildExec("/opt/sat/StatisticsAnalysisTool", "/opt/sat/StatisticsAnalysisTool.dll")
            .Should().Be("\"/opt/sat/StatisticsAnalysisTool\"");
        AutostartEntry.BuildExec("/usr/bin/dotnet", "/opt/sat/StatisticsAnalysisTool.dll")
            .Should().Be("\"/usr/bin/dotnet\" \"/opt/sat/StatisticsAnalysisTool.dll\"");
    }

    [Test]
    public void QuoteExecArgument_EscapesSpecialCharacters()
    {
        AutostartEntry.QuoteExecArgument("/a b/c\"d$e`f\\g%h").Should().Be("\"/a b/c\\\\\"d\\\\$e\\\\`f\\\\\\\\g%%h\"");
    }

    [Test]
    public void BuildContent_AddsTheWorkingDirectory_EscapingBackslashes()
    {
        AutostartEntry.BuildContent("\"/x\"", "/opt/sat").Should().Contain("Path=/opt/sat\n");
        AutostartEntry.BuildContent("\"/x\"", "/a\\b").Should().Contain("Path=/a\\\\b\n");
        AutostartEntry.BuildContent("\"/x\"").Should().NotContain("Path=");
    }

    [Test]
    public void TrySet_LeavesNoTemporaryFilesBehind()
    {
        var root = Directory.CreateTempSubdirectory("sat-autostart").FullName;
        try
        {
            AutostartEntry.TrySet(true, root, "\"/x\"", "/opt/sat").Should().BeTrue();
            Directory.GetFiles(root).Select(Path.GetFileName).Should().Equal(AutostartEntry.FileName);
            File.ReadAllText(Path.Combine(root, AutostartEntry.FileName)).Should().Contain("Path=/opt/sat\n");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void BuildContent_IsAValidApplicationEntry()
    {
        var content = AutostartEntry.BuildContent("\"/opt/sat/StatisticsAnalysisTool\"");

        content.Should().StartWith("[Desktop Entry]\n");
        content.Should().Contain("Type=Application\n").And.Contain("Name=Statistics Analysis Tool\n")
            .And.Contain("Exec=\"/opt/sat/StatisticsAnalysisTool\"\n").And.Contain("Terminal=false\n")
            .And.Contain("X-GNOME-Autostart-enabled=true\n");
    }

    [Test]
    public void TrySet_WritesAndRemovesTheEntry_CreatingTheDirectory()
    {
        var root = Directory.CreateTempSubdirectory("sat-autostart").FullName;
        try
        {
            var dir = Path.Combine(root, "nested", "autostart");

            AutostartEntry.TrySet(true, dir, "\"/x/sat\"").Should().BeTrue();
            File.ReadAllText(Path.Combine(dir, AutostartEntry.FileName)).Should().Contain("Exec=\"/x/sat\"");

            AutostartEntry.TrySet(true, dir, "\"/y/sat\"").Should().BeTrue("enabling twice overwrites");
            File.ReadAllText(Path.Combine(dir, AutostartEntry.FileName)).Should().Contain("Exec=\"/y/sat\"");

            AutostartEntry.TrySet(false, dir, "\"/y/sat\"").Should().BeTrue();
            File.Exists(Path.Combine(dir, AutostartEntry.FileName)).Should().BeFalse();
            AutostartEntry.TrySet(false, dir, "\"/y/sat\"").Should().BeTrue("removing a missing entry succeeds");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void TrySet_UnwritableLocation_ReportsFailure()
    {
        AutostartEntry.TrySet(true, "/proc/definitely/not/writable", "\"/x\"").Should().BeFalse();
    }
}
