// The Libpcap NuGet package P/Invokes the native library "pcap" (its own resolver adds "wpcap" on Windows only).
// .NET then probes libpcap.so / pcap.so / libpcap / pcap, so a machine that only has the runtime package
// (libpcap.so.1, or libpcap.so.0.8 on Debian) fails with DllNotFoundException. The package registers its own
// DllImportResolver for its assembly, and a second registration throws, so this hooks the
// AssemblyLoadContext.ResolvingUnmanagedDll fallback instead: it only runs when the stock probing failed.
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace StatisticsAnalysisTool.Linux.Capture;

internal static class LibpcapLibrary
{
    public const string ImportName = "pcap";

    public static readonly string[] CandidateNames = ["libpcap.so.1", "libpcap.so", "libpcap.so.0.8"];

    private static int _installed;

    public static IntPtr TryLoad(string libraryName, Func<string, IntPtr> loader)
    {
        if (libraryName != ImportName)
        {
            return IntPtr.Zero;
        }

        foreach (var candidate in CandidateNames)
        {
            var handle = loader(candidate);
            if (handle != IntPtr.Zero)
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>Registers the fallback resolver once. Call it before the first libpcap call.</summary>
    public static void Install()
    {
        if (!OperatingSystem.IsLinux() || System.Threading.Interlocked.Exchange(ref _installed, 1) == 1)
        {
            return;
        }

        AssemblyLoadContext.Default.ResolvingUnmanagedDll += OnResolvingUnmanagedDll;
    }

    public static bool IsInstalledOnThisMachine()
    {
        var handle = TryLoad(ImportName, LoadAndKeep);
        return handle != IntPtr.Zero;
    }

    private static IntPtr OnResolvingUnmanagedDll(Assembly assembly, string libraryName)
    {
        return TryLoad(libraryName, LoadAndKeep);
    }

    private static IntPtr LoadAndKeep(string name)
    {
        return NativeLibrary.TryLoad(name, out var handle) ? handle : IntPtr.Zero;
    }
}
