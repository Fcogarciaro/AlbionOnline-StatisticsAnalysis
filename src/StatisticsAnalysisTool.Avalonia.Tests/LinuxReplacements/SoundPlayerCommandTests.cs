using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class SoundPlayerCommandTests
{
    [TestCase(100.0, 1.0)]
    [TestCase(50.0, 0.5)]
    [TestCase(150.0, 1.0)]
    [TestCase(-5.0, 0.0)]
    [TestCase(double.NaN, 1.0)]
    [TestCase(double.PositiveInfinity, 1.0)]
    public void NormalizeVolume_ClampsAndDefaultsToFull(double input, double expected)
    {
        SoundPlayerCommand.NormalizeVolume(input).Should().BeApproximately(expected, 1e-9);
    }

    [Test]
    public void Resolve_PrefersPipeWireThenPulseThenAlsa()
    {
        var all = SoundPlayerCommand.Resolve(_ => true, "/s/a.wav", 100);
        all.Select(c => c.FileName).Should().Equal("pw-play", "paplay", "aplay");

        SoundPlayerCommand.Resolve(n => n != "pw-play", "/s/a.wav", 100).Select(c => c.FileName).Should().Equal("paplay", "aplay");
    }

    [Test]
    public void Resolve_NoPlayerInstalled_IsEmpty()
    {
        SoundPlayerCommand.Resolve(_ => false, "/s/a.wav", 100).Should().BeEmpty();
    }

    [Test]
    public void Resolve_MutedVolume_PlaysNothing()
    {
        SoundPlayerCommand.Resolve(_ => true, "/s/a.wav", 0).Should().BeEmpty();
    }

    [Test]
    public void Resolve_PassesVolumeInEachPlayersScale_AndKeepsPathAsOneArgument()
    {
        var commands = SoundPlayerCommand.Resolve(_ => true, "/my sounds/a b.wav", 50).ToDictionary(c => c.FileName);

        commands["pw-play"].Arguments.Should().Equal("--volume=0.50", "--", "/my sounds/a b.wav");
        commands["paplay"].Arguments.Should().Equal("--volume=32768", "--", "/my sounds/a b.wav");
        commands["aplay"].Arguments.Should().Equal("-q", "--", "/my sounds/a b.wav");
    }

    [Test]
    public void Resolve_BlankPath_IsEmpty()
    {
        SoundPlayerCommand.Resolve(_ => true, " ", 100).Should().BeEmpty();
    }
}
