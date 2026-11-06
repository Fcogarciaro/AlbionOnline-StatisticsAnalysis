using System.Windows;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;

namespace StatisticsAnalysisTool.Avalonia.Tests.Compat;

public class ApplicationResourceStreamTests
{
    [AvaloniaTest]
    public void GetResourceStream_MissingResource_ThrowsIoExceptionLikeWpf()
    {
        var uri = new Uri("pack://application:,,,/Does/Not/Exist.png", UriKind.Absolute);

        var act = () => Application.GetResourceStream(uri);

        act.Should().Throw<IOException>();
    }

    [AvaloniaTest]
    public void GetResourceStream_ExistingAvaresAsset_ReturnsReadableStream()
    {
        var uri = new Uri("avares://StatisticsAnalysisTool.Avalonia.Tests/Fixtures/probe.txt");

        using var info = Application.GetResourceStream(uri);

        info.Stream.Length.Should().BeGreaterThan(0);
    }
}
