using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class AlbionProcessMatcherTests
{
    [TestCase("Albion-Online", null)]
    [TestCase("albion-online", null)]
    [TestCase("Albion-Online.exe", null)]
    [TestCase("Albion-Online.e", null)]
    [TestCase("wine64-preloader", "Z:\\home\\u\\.steam\\steamapps\\common\\Albion Online\\game\\Albion-Online.exe")]
    [TestCase("wine64-preloader", "/games/Albion Online/Albion-Online.exe")]
    [TestCase("Albion-Online\n", "")]
    public void IsMatch_RecognizesNativeAndProtonProcesses(string comm, string argv0)
    {
        AlbionProcessMatcher.IsMatch(comm, argv0).Should().BeTrue();
    }

    [TestCase("AlbionLauncher", null)]
    [TestCase("Albion-Online-Launcher", null)]
    [TestCase("Albion-Onlin", null)]
    [TestCase("bash", "/home/u/Albion-Online.sh")]
    [TestCase("proton", "/steam/proton")]
    [TestCase("", "")]
    [TestCase(null, null)]
    public void IsMatch_RejectsOtherProcesses(string comm, string argv0)
    {
        AlbionProcessMatcher.IsMatch(comm, argv0).Should().BeFalse();
    }

    private static string FakeProc(params (string pid, string comm, string cmdline)[] processes)
    {
        var root = Directory.CreateTempSubdirectory("sat-proc").FullName;
        foreach (var (pid, comm, cmdline) in processes)
        {
            var dir = Directory.CreateDirectory(Path.Combine(root, pid)).FullName;
            File.WriteAllText(Path.Combine(dir, "comm"), comm + "\n");
            File.WriteAllText(Path.Combine(dir, "cmdline"), cmdline);
        }

        Directory.CreateDirectory(Path.Combine(root, "self-not-a-pid"));
        return root;
    }

    [Test]
    public void IsGameRunning_FindsGameUnderProtonViaArgv0()
    {
        var root = FakeProc(("100", "bash", "bash\0-c\0x"), ("200", "wine64-preloader", "Z:\\g\\Albion-Online.exe\0--arg"));
        try { AlbionProcessMatcher.IsGameRunning(root).Should().BeTrue(); }
        finally { Directory.Delete(root, true); }
    }

    [Test]
    public void IsGameRunning_FalseWhenNothingMatches_AndIgnoresBrokenEntries()
    {
        var root = FakeProc(("100", "bash", "bash\0"), ("300", "AlbionLauncher", "/x/AlbionLauncher\0"));
        Directory.CreateDirectory(Path.Combine(root, "400")); // no comm/cmdline files
        try { AlbionProcessMatcher.IsGameRunning(root).Should().BeFalse(); }
        finally { Directory.Delete(root, true); }
    }

    [Test]
    public void IsGameRunning_MissingRoot_IsFalse()
    {
        AlbionProcessMatcher.IsGameRunning("/nonexistent/proc").Should().BeFalse();
    }
}
