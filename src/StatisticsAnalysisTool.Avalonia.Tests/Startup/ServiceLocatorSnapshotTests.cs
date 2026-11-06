using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Common;

namespace StatisticsAnalysisTool.Avalonia.Tests.Startup;

[NonParallelizable]
public class ServiceLocatorSnapshotTests
{
    private sealed class Marker;

    [Test]
    public void Dispose_RemovesServicesRegisteredAfterTheSnapshot()
    {
        using (new ServiceLocatorSnapshot())
        {
            ServiceLocator.Register<Marker>(new Marker());
            ServiceLocator.IsServiceInDictionary<Marker>().Should().BeTrue();
        }

        ServiceLocator.IsServiceInDictionary<Marker>().Should().BeFalse("a test must not leak its services into the next test");
    }

    [Test]
    public void Dispose_RestoresServicesThatWereReplaced()
    {
        var original = new Marker();
        ServiceLocator.Register<Marker>(original);
        try
        {
            using (new ServiceLocatorSnapshot())
            {
                ServiceLocator.Register<Marker>(new Marker());
            }

            ServiceLocator.Resolve<Marker>().Should().BeSameAs(original);
        }
        finally
        {
            ServiceLocatorSnapshot.Remove<Marker>();
        }
    }
}
