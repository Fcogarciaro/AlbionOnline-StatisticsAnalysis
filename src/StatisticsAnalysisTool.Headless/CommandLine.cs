using System;
using System.Collections.Generic;

namespace StatisticsAnalysisTool.Headless;

public sealed record CommandLineOptions(IReadOnlyList<string> Devices, bool ShowHelp, bool ShowVersion, string Error)
{
    public static CommandLineOptions Parse(string[] args)
    {
        var devices = new List<string>();
        var help = false;
        var version = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h":
                case "--help":
                    help = true;
                    break;
                case "--version":
                    version = true;
                    break;
                case "--device":
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        return new CommandLineOptions(devices, help, version, "--device needs an interface name");
                    }

                    devices.Add(args[++i]);
                    break;
                default:
                    if (args[i].StartsWith("--device=", StringComparison.Ordinal) && args[i].Length > "--device=".Length)
                    {
                        devices.Add(args[i]["--device=".Length..]);
                        break;
                    }

                    return new CommandLineOptions(devices, help, version, $"unknown argument: {args[i]}");
            }
        }

        return new CommandLineOptions(devices, help, version, null);
    }

    public const string Usage = """
        sat-headless - Albion Online event stream (JSON lines on stdout, diagnostics on stderr)

        Usage: sat-headless [--device <interface>]... [--help] [--version]

          --device <name>   capture only this interface (repeatable); default: every up, non-loopback interface
          --help            show this text
          --version         print the version

        Needs CAP_NET_RAW (or root). Grant it once with:
          sudo setcap cap_net_raw,cap_net_admin=eip <path to sat-headless>
        """;
}
