// Out-of-process file pickers (zenity, kdialog). A process with file capabilities (setcap for packet capture) is
// non-dumpable, so xdg-desktop-portal cannot open /proc/<pid>/root and rejects every portal picker call. File
// capabilities are not inherited across exec, so a child process can use the portal or GTK/Qt normally.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using Serilog;

namespace StatisticsAnalysisTool.Linux.Platform;

internal enum PickerKind
{
    Folder,
    OpenFile,
    SaveFile
}

internal enum PickerTool
{
    Zenity,
    Kdialog
}

internal sealed class PickerRequest
{
    public PickerKind Kind { get; init; }

    public string Title { get; init; }

    public string InitialPath { get; init; }

    public string SuggestedName { get; init; }

    public bool Multiple { get; init; }

    public IReadOnlyList<FilterEntry> Filters { get; init; } = [];
}

internal readonly record struct ProcessOutput(int ExitCode, string StdOut);

internal interface IProcessRunner
{
    Task<ProcessOutput> RunAsync(string fileName, IReadOnlyList<string> arguments);
}

/// <summary>Runs a child process without a shell (ArgumentList) and captures stdout.</summary>
internal sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessOutput> RunAsync(string fileName, IReadOnlyList<string> arguments)
    {
        var info = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start(info) ?? throw new Win32Exception($"Could not start {fileName}");
        var stdout = process.StandardOutput.ReadToEndAsync();
        _ = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        return new ProcessOutput(process.ExitCode, await stdout.ConfigureAwait(false));
    }
}

internal sealed class ExternalFilePicker(Func<string, bool> toolExists, IProcessRunner runner)
{
    public static ExternalFilePicker CreateDefault() => new(ExistsOnPath, new ProcessRunner());

    public bool IsAvailable => Detect() != null;

    /// <summary>
    /// The selected paths; empty when the user cancelled; null when no tool exists or it could not be started,
    /// so the caller can try something else.
    /// </summary>
    public async Task<IReadOnlyList<string>> PickAsync(PickerRequest request)
    {
        var tool = Detect();
        if (tool == null)
        {
            return null;
        }

        var fileName = tool.Value == PickerTool.Zenity ? "zenity" : "kdialog";
        try
        {
            var output = await runner.RunAsync(fileName, BuildArguments(tool.Value, request)).ConfigureAwait(false);
            if (output.ExitCode > 1)
            {
                Log.Warning("{Tool} exited with code {Code}; treating the dialog as cancelled", fileName, output.ExitCode);
            }

            return ParseOutput(output.StdOut, output.ExitCode, request.Multiple);
        }
        catch (Exception e) when (e is Win32Exception or IOException or InvalidOperationException)
        {
            Log.Warning(e, "Could not run {Tool}", fileName);
            return null;
        }
    }

    private PickerTool? Detect()
    {
        if (toolExists("zenity"))
        {
            return PickerTool.Zenity;
        }

        return toolExists("kdialog") ? PickerTool.Kdialog : null;
    }

    public static IReadOnlyList<string> BuildArguments(PickerTool tool, PickerRequest request)
        => tool == PickerTool.Zenity ? BuildZenity(request) : BuildKdialog(request);

    /// <summary>Exit code 1 or empty output means cancelled; any other non-zero code is treated the same.</summary>
    public static IReadOnlyList<string> ParseOutput(string stdout, int exitCode, bool multiple)
    {
        if (exitCode != 0 || string.IsNullOrEmpty(stdout))
        {
            return [];
        }

        var lines = stdout.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0);
        return multiple ? lines.ToList() : lines.Take(1).ToList();
    }

    private static List<string> BuildZenity(PickerRequest request)
    {
        var args = new List<string> { "--file-selection" };
        switch (request.Kind)
        {
            case PickerKind.Folder:
                args.Add("--directory");
                break;
            case PickerKind.SaveFile:
                args.Add("--save");
                args.Add("--confirm-overwrite");
                break;
        }

        if (request.Multiple && request.Kind != PickerKind.SaveFile)
        {
            args.Add("--multiple");
            args.Add("--separator=\n");
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            args.Add($"--title={request.Title}");
        }

        if (request.Kind != PickerKind.Folder)
        {
            args.AddRange(request.Filters.Select(f => $"--file-filter={f.Name} | {string.Join(' ', f.Patterns)}"));
        }

        var start = StartPath(request);
        if (start.Length > 0)
        {
            args.Add($"--filename={start}");
        }

        return args;
    }

    private static List<string> BuildKdialog(PickerRequest request)
    {
        var args = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            args.Add("--title");
            args.Add(request.Title);
        }

        var start = StartPath(request);
        if (start.Length > 1)
        {
            start = start.TrimEnd('/');
        }

        if (start.Length == 0)
        {
            start = request.Kind == PickerKind.SaveFile ? "." : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (request.Kind == PickerKind.Folder)
        {
            args.Add("--getexistingdirectory");
            args.Add(start);
            return args;
        }

        if (request.Kind == PickerKind.OpenFile && request.Multiple)
        {
            args.Add("--multiple");
            args.Add("--separate-output");
        }

        args.Add(request.Kind == PickerKind.SaveFile ? "--getsavefilename" : "--getopenfilename");
        args.Add(start);
        if (request.Filters.Count > 0)
        {
            args.Add(string.Join('\n', request.Filters.Select(f => $"{string.Join(' ', f.Patterns)}|{f.Name}")));
        }

        return args;
    }

    /// <summary>Directory with a trailing slash, or directory + suggested name for save dialogs.</summary>
    private static string StartPath(PickerRequest request)
    {
        var directory = string.IsNullOrWhiteSpace(request.InitialPath) ? string.Empty : request.InitialPath.TrimEnd('/') + "/";
        if (request.Kind == PickerKind.SaveFile && !string.IsNullOrWhiteSpace(request.SuggestedName))
        {
            return directory + request.SuggestedName;
        }

        return directory;
    }

    private static bool ExistsOnPath(string tool)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return path.Split(':', StringSplitOptions.RemoveEmptyEntries).Any(dir => File.Exists(Path.Combine(dir, tool)));
    }
}
