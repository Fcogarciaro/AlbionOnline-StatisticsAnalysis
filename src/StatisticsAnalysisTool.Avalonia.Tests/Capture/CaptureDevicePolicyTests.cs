using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Capture;

namespace StatisticsAnalysisTool.Avalonia.Tests.Capture;

public class CaptureDevicePolicyTests
{
    [TestCase("any", true)]
    [TestCase("enp1s0", false)]
    [TestCase("Any", false)]
    [TestCase("", false)]
    public void IsAnyDevice_MatchesOnlyThePseudoDevice(string name, bool expected)
    {
        CaptureDevicePolicy.IsAnyDevice(name).Should().Be(expected);
    }
}
