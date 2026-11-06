using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.Platform;

public class PickerRouterTests
{
    private sealed class FakeRunner(ProcessOutput output) : IProcessRunner
    {
        public int Calls { get; private set; }

        public Task<ProcessOutput> RunAsync(string fileName, IReadOnlyList<string> arguments)
        {
            Calls++;
            return Task.FromResult(output);
        }
    }

    private static readonly PickerRequest Request = new() { Kind = PickerKind.Folder };

    private static ExternalFilePicker WithTool(FakeRunner runner) => new(_ => true, runner);

    private static ExternalFilePicker WithoutTool() => new(_ => false, new FakeRunner(new ProcessOutput(0, "/never\n")));

    [Test]
    public async Task NonDumpable_UsesExternalFirst_AndSkipsProvider()
    {
        var providerCalls = 0;
        var outcome = await PickerRouter.PickAsync(Request, () => { providerCalls++; return Task.FromResult<IReadOnlyList<string>>(["/p"]); },
            WithTool(new FakeRunner(new ProcessOutput(0, "/ext\n"))), preferExternal: true);

        outcome.Paths.Should().Equal("/ext");
        outcome.ToolMissing.Should().BeFalse();
        providerCalls.Should().Be(0);
    }

    [Test]
    public async Task NonDumpable_ExternalCancelled_DoesNotFallBackToProvider()
    {
        var providerCalls = 0;
        var outcome = await PickerRouter.PickAsync(Request, () => { providerCalls++; return Task.FromResult<IReadOnlyList<string>>(["/p"]); },
            WithTool(new FakeRunner(new ProcessOutput(1, ""))), preferExternal: true);

        outcome.Paths.Should().BeEmpty();
        providerCalls.Should().Be(0);
    }

    [Test]
    public async Task NonDumpable_NoTool_TriesProvider()
    {
        var outcome = await PickerRouter.PickAsync(Request, () => Task.FromResult<IReadOnlyList<string>>(["/p"]), WithoutTool(), preferExternal: true);

        outcome.Paths.Should().Equal("/p");
        outcome.ToolMissing.Should().BeFalse();
    }

    [Test]
    public async Task Dumpable_UsesProvider_AndNeverTouchesExternal()
    {
        var runner = new FakeRunner(new ProcessOutput(0, "/ext\n"));
        var outcome = await PickerRouter.PickAsync(Request, () => Task.FromResult<IReadOnlyList<string>>(["/p"]), WithTool(runner), preferExternal: false);

        outcome.Paths.Should().Equal("/p");
        runner.Calls.Should().Be(0);
    }

    [Test]
    public async Task Dumpable_ProviderCancel_IsNotRetriedExternally()
    {
        var runner = new FakeRunner(new ProcessOutput(0, "/ext\n"));
        var outcome = await PickerRouter.PickAsync(Request, () => Task.FromResult<IReadOnlyList<string>>([]), WithTool(runner), preferExternal: false);

        outcome.Paths.Should().BeEmpty();
        runner.Calls.Should().Be(0);
    }

    [Test]
    public async Task ProviderThrows_FallsBackToExternal()
    {
        var runner = new FakeRunner(new ProcessOutput(0, "/ext\n"));
        var outcome = await PickerRouter.PickAsync(Request, () => throw new InvalidOperationException("AccessDenied"), WithTool(runner), preferExternal: false);

        outcome.Paths.Should().Equal("/ext");
        runner.Calls.Should().Be(1);
    }

    [Test]
    public async Task ProviderThrowsAsynchronously_FallsBackToExternal()
    {
        var outcome = await PickerRouter.PickAsync(Request, async () => { await Task.Yield(); throw new InvalidOperationException("x"); },
            WithTool(new FakeRunner(new ProcessOutput(0, "/ext\n"))), preferExternal: false);

        outcome.Paths.Should().Equal("/ext");
    }

    [Test]
    public async Task ProviderThrows_NoTool_ReturnsEmptyAndFlagsMissingTool()
    {
        var outcome = await PickerRouter.PickAsync(Request, () => throw new InvalidOperationException("x"), WithoutTool(), preferExternal: false);

        outcome.Paths.Should().BeEmpty();
        outcome.ToolMissing.Should().BeTrue();
    }

    [Test]
    public async Task NonDumpable_NoTool_ProviderThrows_FlagsMissingTool()
    {
        var outcome = await PickerRouter.PickAsync(Request, () => throw new InvalidOperationException("x"), WithoutTool(), preferExternal: true);

        outcome.Paths.Should().BeEmpty();
        outcome.ToolMissing.Should().BeTrue();
    }

    [TestCase(0, "", true)]
    [TestCase(2, "", true)]
    [TestCase(1, "CapEff:\t0000000000003000\n", false)]
    [TestCase(-1, "CapEff:\t0000000000003000\n", true)]
    [TestCase(-1, "CapEff:\t0000000000000000\n", false)]
    [TestCase(-1, "", false)]
    public void IsNonDumpable_Decision(int prctlResult, string procStatus, bool expected)
        => ProcessDumpability.IsNonDumpable(prctlResult, procStatus).Should().Be(expected);
}
