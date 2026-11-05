using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common;

namespace StatisticsAnalysisTool.Avalonia.Tests.LinuxReplacements;

public class SystemInfoTests
{
    [Test]
    public void ParseProcessorName_ReadsFirstModelNameLine()
    {
        const string cpuInfo = "processor\t: 0\nmodel name\t: AMD Ryzen 7 5800X 8-Core Processor\ncpu MHz\t\t: 3800\nmodel name\t: second\n";

        SystemInfo.ParseProcessorName(cpuInfo).Should().Be("AMD Ryzen 7 5800X 8-Core Processor");
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("processor : 0")]
    public void ParseProcessorName_MissingModelName_ReturnsUnknown(string cpuInfo)
    {
        SystemInfo.ParseProcessorName(cpuInfo).Should().Be("Unknown");
    }

    [Test]
    public void CreateReport_ContainsLinuxFriendlySections()
    {
        SystemInfo.CreateReport().Should().Contain("OS version:").And.Contain("Processor:").And.Contain("Is admin:");
    }
}
