using FluentAssertions;
using Microsoft.Win32;
using NUnit.Framework;
using StatisticsAnalysisTool.Linux.Platform;

namespace StatisticsAnalysisTool.Avalonia.Tests.Platform;

public class ExternalFilePickerTests
{
    private static readonly FilterEntry[] CsvAndAll =
    [
        new("CSV (.csv)", ["*.csv"]),
        new("All files", ["*.*"])
    ];

    private sealed class FakeRunner(ProcessOutput output) : IProcessRunner
    {
        public string File { get; private set; }
        public IReadOnlyList<string> Args { get; private set; }
        public int Calls { get; private set; }

        public Task<ProcessOutput> RunAsync(string fileName, IReadOnlyList<string> arguments)
        {
            Calls++;
            File = fileName;
            Args = arguments;
            return Task.FromResult(output);
        }
    }

    private sealed class ThrowingRunner : IProcessRunner
    {
        public Task<ProcessOutput> RunAsync(string fileName, IReadOnlyList<string> arguments)
            => throw new System.ComponentModel.Win32Exception("not found");
    }

    [Test]
    public void ZenityFolder_Arguments()
    {
        var request = new PickerRequest { Kind = PickerKind.Folder, Title = "Game folder", InitialPath = "/games/albion" };

        ExternalFilePicker.BuildArguments(PickerTool.Zenity, request)
            .Should().Equal("--file-selection", "--directory", "--title=Game folder", "--filename=/games/albion/");
    }

    [Test]
    public void ZenityOpenMultiple_Arguments()
    {
        var request = new PickerRequest { Kind = PickerKind.OpenFile, Multiple = true, Filters = CsvAndAll, InitialPath = "/data" };

        ExternalFilePicker.BuildArguments(PickerTool.Zenity, request).Should().Equal(
            "--file-selection", "--multiple", "--separator=\n", "--file-filter=CSV (.csv) | *.csv",
            "--file-filter=All files | *.*", "--filename=/data/");
    }

    [Test]
    public void ZenitySave_Arguments()
    {
        var request = new PickerRequest
        {
            Kind = PickerKind.SaveFile, Title = "Export", SuggestedName = "log.csv", InitialPath = "/data", Filters = CsvAndAll
        };

        ExternalFilePicker.BuildArguments(PickerTool.Zenity, request).Should().Equal(
            "--file-selection", "--save", "--confirm-overwrite", "--title=Export", "--file-filter=CSV (.csv) | *.csv",
            "--file-filter=All files | *.*", "--filename=/data/log.csv");
    }

    [Test]
    public void KdialogFolder_Arguments()
    {
        var request = new PickerRequest { Kind = PickerKind.Folder, Title = "Game folder", InitialPath = "/games" };

        ExternalFilePicker.BuildArguments(PickerTool.Kdialog, request)
            .Should().Equal("--title", "Game folder", "--getexistingdirectory", "/games");
    }

    [Test]
    public void KdialogOpenMultiple_Arguments()
    {
        var request = new PickerRequest { Kind = PickerKind.OpenFile, Multiple = true, Filters = CsvAndAll, InitialPath = "/data" };

        ExternalFilePicker.BuildArguments(PickerTool.Kdialog, request).Should().Equal(
            "--multiple", "--separate-output", "--getopenfilename", "/data", "*.csv|CSV (.csv)\n*.*|All files");
    }

    [Test]
    public void KdialogSave_Arguments()
    {
        var request = new PickerRequest { Kind = PickerKind.SaveFile, SuggestedName = "log.csv", InitialPath = "/data", Filters = CsvAndAll };

        ExternalFilePicker.BuildArguments(PickerTool.Kdialog, request).Should().Equal(
            "--getsavefilename", "/data/log.csv", "*.csv|CSV (.csv)\n*.*|All files");
    }

    [Test]
    public void BuildArguments_EmptyInitialPath_OmitsFilename()
    {
        var request = new PickerRequest { Kind = PickerKind.OpenFile };

        ExternalFilePicker.BuildArguments(PickerTool.Zenity, request).Should().Equal("--file-selection");
    }

    [Test]
    public void ParseOutput_Single()
        => ExternalFilePicker.ParseOutput("/a/b.txt\n", 0, multiple: false).Should().Equal("/a/b.txt");

    [Test]
    public void ParseOutput_Multiple_SplitsOnNewlineAndKeepsSpacesAndPipes()
        => ExternalFilePicker.ParseOutput("/a/b c.txt\n/a/d|e.txt\r\n", 0, multiple: true).Should().Equal("/a/b c.txt", "/a/d|e.txt");

    [Test]
    public void ParseOutput_SingleMode_TakesFirstLineOnly()
        => ExternalFilePicker.ParseOutput("/a\n/b\n", 0, multiple: false).Should().Equal("/a");

    [TestCase(1, "")]
    [TestCase(0, "")]
    [TestCase(0, "\n")]
    [TestCase(5, "/a\n")]
    public void ParseOutput_CancelledOrFailed_IsEmpty(int exitCode, string stdout)
        => ExternalFilePicker.ParseOutput(stdout, exitCode, multiple: true).Should().BeEmpty();

    [Test]
    public async Task PickAsync_PrefersZenityOverKdialog()
    {
        var runner = new FakeRunner(new ProcessOutput(0, "/x\n"));
        var picker = new ExternalFilePicker(_ => true, runner);

        var result = await picker.PickAsync(new PickerRequest { Kind = PickerKind.Folder });

        result.Should().Equal("/x");
        runner.File.Should().Be("zenity");
    }

    [Test]
    public async Task PickAsync_FallsBackToKdialog()
    {
        var runner = new FakeRunner(new ProcessOutput(0, "/x\n"));
        var picker = new ExternalFilePicker(name => name == "kdialog", runner);

        await picker.PickAsync(new PickerRequest { Kind = PickerKind.Folder });

        runner.File.Should().Be("kdialog");
    }

    [Test]
    public async Task PickAsync_NoTool_ReturnsNullWithoutRunning()
    {
        var runner = new FakeRunner(new ProcessOutput(0, "/x\n"));
        var picker = new ExternalFilePicker(_ => false, runner);

        var result = await picker.PickAsync(new PickerRequest { Kind = PickerKind.Folder });

        result.Should().BeNull();
        runner.Calls.Should().Be(0);
        picker.IsAvailable.Should().BeFalse();
    }

    [Test]
    public async Task PickAsync_Cancelled_ReturnsEmptyNotNull()
    {
        var picker = new ExternalFilePicker(_ => true, new FakeRunner(new ProcessOutput(1, "")));

        var result = await picker.PickAsync(new PickerRequest { Kind = PickerKind.OpenFile });

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public async Task PickAsync_ProcessCannotStart_ReturnsNull()
    {
        var picker = new ExternalFilePicker(_ => true, new ThrowingRunner());

        var result = await picker.PickAsync(new PickerRequest { Kind = PickerKind.OpenFile });

        result.Should().BeNull();
    }
}
