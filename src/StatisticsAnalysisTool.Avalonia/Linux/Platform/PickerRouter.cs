// Decides between the Avalonia (portal) picker and the external zenity/kdialog picker.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;
using StatisticsAnalysisTool.Linux.Capture;

namespace StatisticsAnalysisTool.Linux.Platform;

internal readonly record struct PickerOutcome(IReadOnlyList<string> Paths, bool ToolMissing);

internal static class PickerRouter
{
    public const string MissingToolMessage =
        "The system file dialog is not available to this application (the portal rejects processes with file capabilities). " +
        "Install zenity (or kdialog) so the tool can open file and folder pickers, e.g. 'sudo pacman -S zenity'.";

    /// <summary>
    /// Non-dumpable processes (setcap) try the external picker first; a throwing provider falls back to it too.
    /// Never throws: with no usable picker the outcome is empty and flags the missing tool.
    /// </summary>
    public static async Task<PickerOutcome> PickAsync(
        PickerRequest request, Func<Task<IReadOnlyList<string>>> viaProvider, ExternalFilePicker external, bool preferExternal)
    {
        var externalTried = false;
        if (preferExternal)
        {
            externalTried = true;
            var first = await external.PickAsync(request);
            if (first != null)
            {
                return new PickerOutcome(first, false);
            }
        }

        try
        {
            return new PickerOutcome(await viaProvider(), false);
        }
        catch (Exception e)
        {
            Log.Warning(e, "The platform {Kind} picker failed; falling back to an external dialog", request.Kind);
        }

        var fallback = externalTried ? null : await external.PickAsync(request);
        if (fallback != null)
        {
            return new PickerOutcome(fallback, false);
        }

        Log.Warning(MissingToolMessage);
        return new PickerOutcome([], true);
    }
}

internal static class ProcessDumpability
{
    private const int PrGetDumpable = 3;

    [DllImport("libc", EntryPoint = "prctl")]
    private static extern int Prctl(int option, ulong arg2, ulong arg3, ulong arg4, ulong arg5);

    /// <summary>
    /// A non-dumpable process (PR_GET_DUMPABLE != 1) cannot be inspected through /proc/&lt;pid&gt;/root, which the
    /// portal requires. When prctl fails, a non-zero CapEff of /proc/self/status is the fallback hint.
    /// </summary>
    public static bool IsNonDumpable(int prctlResult, string procStatus)
    {
        if (prctlResult >= 0)
        {
            return prctlResult != 1;
        }

        var capEff = CapturePrivilege.ParseCapEff(procStatus);
        return capEff is > 0;
    }

    public static bool IsCurrentProcessNonDumpable()
    {
        try
        {
            var result = Prctl(PrGetDumpable, 0, 0, 0, 0);
            return IsNonDumpable(result, result >= 0 ? null : File.ReadAllText("/proc/self/status"));
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
