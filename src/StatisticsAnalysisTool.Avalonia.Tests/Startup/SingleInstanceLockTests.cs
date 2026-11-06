using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Startup;

namespace StatisticsAnalysisTool.Avalonia.Tests.Startup;

public class SingleInstanceLockTests
{
    private string _directory;

    [SetUp]
    public void SetUp() => _directory = Directory.CreateTempSubdirectory("sat-lock").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_directory, true);

    [Test]
    public void SecondAcquireForTheSameInstance_IsRefused_UntilTheFirstIsReleased()
    {
        var first = SingleInstanceLock.TryAcquire(_directory, "abc");
        first.Should().NotBeNull();

        SingleInstanceLock.TryAcquire(_directory, "abc").Should().BeNull();

        first.Dispose();
        using var again = SingleInstanceLock.TryAcquire(_directory, "abc");
        again.Should().NotBeNull();
    }

    [Test]
    public void DifferentInstanceIds_CoexistLikeSeparateInstallations()
    {
        using var a = SingleInstanceLock.TryAcquire(_directory, "aaaa");
        using var b = SingleInstanceLock.TryAcquire(_directory, "bbbb");

        a.Should().NotBeNull();
        b.Should().NotBeNull();
    }

    [Test]
    public void TryAcquire_CreatesTheDirectory_AndSanitizesTheInstanceId()
    {
        var nested = Path.Combine(_directory, "run", "sat");
        using var held = SingleInstanceLock.TryAcquire(nested, "../evil/id");

        held.Should().NotBeNull();
        Directory.GetFiles(nested).Should().ContainSingle().Which.Should().StartWith(nested);
    }

    [Test]
    public void TryAcquire_UnwritableDirectory_DoesNotBlockStartup()
    {
        // Failing to create the lock must not stop the tool from starting (returns a no-op handle).
        using var held = SingleInstanceLock.TryAcquire("/proc/definitely/not/writable", "abc");

        held.Should().NotBeNull();
    }

    [Test]
    public void ResolveDirectory_PrefersAnAbsoluteRuntimeDir_ThenTheCacheRoot()
    {
        string Env(string name) => name == "XDG_RUNTIME_DIR" ? "/run/user/1000" : null;
        SingleInstanceLock.ResolveDirectory(Env, "/cache/sat").Should().Be("/run/user/1000/StatisticsAnalysisTool");

        SingleInstanceLock.ResolveDirectory(_ => "relative", "/cache/sat").Should().Be("/cache/sat");
        SingleInstanceLock.ResolveDirectory(_ => null, "/cache/sat").Should().Be("/cache/sat");
    }
}
