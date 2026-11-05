using Microsoft.Win32;
using FluentAssertions;
using NUnit.Framework;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class FilePickerFilterTests
{
    [Test]
    public void Parse_SinglePair()
    {
        var entries = FilePickerFilter.Parse("CSV documents (.csv)|*.csv");

        entries.Should().ContainSingle();
        entries[0].Name.Should().Be("CSV documents (.csv)");
        entries[0].Patterns.Should().Equal("*.csv");
    }

    [Test]
    public void Parse_MultiplePairsAndSemicolonPatterns()
    {
        var entries = FilePickerFilter.Parse("Images|*.png;*.jpg|All files|*.*");

        entries.Select(e => e.Name).Should().Equal("Images", "All files");
        entries[0].Patterns.Should().Equal("*.png", "*.jpg");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("dangling")]
    public void Parse_EmptyOrMalformed_YieldsNoEntries(string filter)
    {
        FilePickerFilter.Parse(filter).Should().BeEmpty();
    }

    [TestCase("log", ".csv", "log.csv")]
    [TestCase("log", "csv", "log.csv")]
    [TestCase("log.txt", ".csv", "log.txt")]
    [TestCase("log", "", "log")]
    [TestCase("log", null, "log")]
    [TestCase("/tmp/a.b/log", ".json", "/tmp/a.b/log.json")]
    public void EnsureExtension_AppendsOnlyWhenMissing(string name, string ext, string expected)
    {
        FilePickerFilter.EnsureExtension(name, ext).Should().Be(expected);
    }
}
