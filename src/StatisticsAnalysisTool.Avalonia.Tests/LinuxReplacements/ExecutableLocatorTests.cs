using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class ExecutableLocatorTests
{
    private string _dir;

    [SetUp]
    public void SetUp() => _dir = Directory.CreateTempSubdirectory("sat-path").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, true);

    private string MakeFile(string name, UnixFileMode mode)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, "#!/bin/sh\n");
        File.SetUnixFileMode(path, mode);
        return path;
    }

    [Test]
    public void FindsAnExecutableOnAnyPathEntry()
    {
        MakeFile("pw-play", UnixFileMode.UserRead | UnixFileMode.UserExecute);

        ExecutableLocator.IsAvailable("pw-play", $"/nonexistent:{_dir}").Should().BeTrue();
    }

    [Test]
    public void IgnoresNonExecutableFilesMissingFilesAndEmptyPath()
    {
        MakeFile("paplay", UnixFileMode.UserRead);

        ExecutableLocator.IsAvailable("paplay", _dir).Should().BeFalse();
        ExecutableLocator.IsAvailable("aplay", _dir).Should().BeFalse();
        ExecutableLocator.IsAvailable("aplay", null).Should().BeFalse();
        ExecutableLocator.IsAvailable("aplay", "").Should().BeFalse();
    }
}
