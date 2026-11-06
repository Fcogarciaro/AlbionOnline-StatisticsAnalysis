using System.Windows;
using FluentAssertions;
using NUnit.Framework;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat;

// Besides pinning the WPF numeric values, this plain [Test] also lets the NUnit adapter discover the
// [AvaloniaTest] cases in this assembly (it skips assemblies that reference no plain test attribute).
public class VisibilityTests
{
    [Test]
    public void Visibility_UsesWpfNumericValues()
    {
        ((int) Visibility.Visible).Should().Be(0);
        ((int) Visibility.Hidden).Should().Be(1);
        ((int) Visibility.Collapsed).Should().Be(2);
    }
}
