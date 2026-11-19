using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Libpcap;
using Serilog;
using Serilog.Events;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Headless.Capture;
using StatisticsAnalysisTool.Headless.Contract;
using StatisticsAnalysisTool.Headless.Mapping;
using StatisticsAnalysisTool.Linux.Capture;
using StatisticsAnalysisTool.Network;
using StatisticsAnalysisTool.Network.PacketProviders;

namespace StatisticsAnalysisTool.Headless;

internal static class Program
{
    private const int ExitUsage = 64;
    private const int ExitCaptureFailed = 1;

    private static int Main(string[] args)
    {
        var options = CommandLineOptions.Parse(args);
        if (options.Error != null)
        {
            Console.Error.WriteLine(options.Error);
            Console.Error.WriteLine(CommandLineOptions.Usage);
            return ExitUsage;
        }

        if (options.ShowHelp)
        {
            Console.Out.WriteLine(CommandLineOptions.Usage);
            return 0;
        }

        if (options.ShowVersion)
        {
            Console.Out.WriteLine(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown");
            return 0;
        }

        // stdout belongs to the contract: keep a private writer for it and route every stray Console.Out use to stderr.
        var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = false, NewLine = "\n" };
        Console.SetOut(Console.Error);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose)
            .CreateLogger();

        var sink = new JsonLineSink(stdout);
        var binaryPath = Environment.ProcessPath ?? "sat-headless";

        var privilegeExit = StartupCheck.Run(CapturePrivilege.IsCurrentProcessCapable(), binaryPath, sink);
        if (privilegeExit != 0)
        {
            return privilegeExit;
        }

        return Run(options, sink);
    }

    private static int Run(CommandLineOptions options, IEventSink sink)
    {
        using var stop = new ManualResetEventSlim();
        using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => { context.Cancel = true; stop.Set(); });
        using var sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, context => { context.Cancel = true; stop.Set(); });

        PacketProvider provider = null;
        try
        {
            LibpcapLibrary.Install();
            ApplyDeviceSelection(options);

            var gameFiles = GameFilesLocator.Directory();
            var items = ItemCatalog.LoadFrom(gameFiles);
            var world = WorldCatalog.LoadFrom(gameFiles);
            Log.Information("Game files in {Directory}: items {ItemsState}, world {WorldState}", gameFiles,
                ReferenceEquals(items, ItemCatalog.Empty) ? "missing" : "loaded",
                ReferenceEquals(world, WorldCatalog.Empty) ? "missing" : "loaded");

            var mapper = new EventMapper(sink, items, world, NullValueEstimator.Instance);
            var receiver = PhotonReceiverFactory.Build(mapper);
            provider = new LibpcapPacketProvider(receiver, new AlbionServerDetectionService());

            var started = provider.Start();
            if (!started.IsSuccessful)
            {
                sink.Emit(new StatusMessage(StatusStates.Error, "No usable network interface could be opened for capture."));
                return ExitCaptureFailed;
            }

            sink.Emit(new StatusMessage(StatusStates.Capturing, $"Capturing on {started.ActiveCaptureSourceCount} interface(s)."));
            stop.Wait();
        }
        catch (DllNotFoundException e)
        {
            Log.Error(e, "libpcap could not be loaded");
            sink.Emit(new StatusMessage(StatusStates.Error, CaptureGuidance.LibraryMissingMessage));
            return ExitCaptureFailed;
        }
        catch (Exception e)
        {
            Log.Error(e, "Capture failed");
            sink.Emit(new StatusMessage(StatusStates.Error, e.Message));
            return ExitCaptureFailed;
        }
        finally
        {
            try
            {
                provider?.Stop();
            }
            catch (Exception e)
            {
                Log.Warning(e, "Stopping the capture failed");
            }
        }

        sink.Emit(new StatusMessage(StatusStates.Stopped, "Capture stopped."));
        Log.CloseAndFlush();
        return 0;
    }

    /// <summary>With --device only the named interfaces stay selected, every other listed device is switched off.</summary>
    private static void ApplyDeviceSelection(CommandLineOptions options)
    {
        if (options.Devices.Count == 0)
        {
            return;
        }

        var selected = options.Devices.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var devices = Pcap.ListDevices();
        var known = devices.Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var missing in selected.Where(name => !known.Contains(name)))
        {
            Log.Warning("Requested interface {Name} was not found", missing);
        }

        SettingsController.CurrentSettings.NetworkDevices = devices
            .Select(d => new NetworkDeviceSettingsObject { Identifier = d.Name, Name = d.Name, IsSelected = selected.Contains(d.Name) })
            .ToList();
    }
}
