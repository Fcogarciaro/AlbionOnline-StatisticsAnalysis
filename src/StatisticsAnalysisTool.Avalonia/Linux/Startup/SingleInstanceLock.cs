// Upstream has no single-instance guard: instances are told apart only by AppInstance.InstanceId (a hash of the
// install directory), and every instance owns the data directory of its id. Two processes with the same id would
// write the same settings, user data and logs, so the Linux port refuses the second one. The lock is an exclusive
// file (.NET maps FileShare.None to flock), so the kernel releases it when the process dies, however it dies.
using Serilog;
using System;
using System.IO;
using System.Linq;

namespace StatisticsAnalysisTool.Linux.Startup;

public static class SingleInstanceLock
{
    /// <summary>$XDG_RUNTIME_DIR/StatisticsAnalysisTool when absolute (per-login, cleaned on logout), else the cache root.</summary>
    public static string ResolveDirectory(Func<string, string> getEnvironmentVariable, string cacheRoot)
    {
        var runtime = getEnvironmentVariable("XDG_RUNTIME_DIR");
        return !string.IsNullOrEmpty(runtime) && Path.IsPathRooted(runtime)
            ? Path.Combine(runtime, "StatisticsAnalysisTool")
            : cacheRoot;
    }

    /// <summary>
    /// Takes the lock of one instance id. Returns null when another process holds it. When the lock file cannot be
    /// created at all (read-only runtime dir) startup must not be blocked, so a no-op handle is returned.
    /// </summary>
    public static IDisposable TryAcquire(string directory, string instanceId)
    {
        try
        {
            Directory.CreateDirectory(directory);
            return new FileStream(LockPath(directory, instanceId), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return IsHeldElsewhere(directory, instanceId) ? null : FailOpen(e);
        }
    }

    private static string LockPath(string directory, string instanceId)
    {
        var safeId = new string(instanceId.Where(char.IsAsciiLetterOrDigit).ToArray());
        return Path.Combine(directory, $"instance-{(safeId.Length == 0 ? "default" : safeId)}.lock");
    }

    // The file opens with sharing allowed but not exclusively: another process holds the lock.
    private static bool IsHeldElsewhere(string directory, string instanceId)
    {
        try
        {
            var path = LockPath(directory, instanceId);
            if (!File.Exists(path))
            {
                return false;
            }

            using var probe = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static IDisposable FailOpen(Exception exception)
    {
        Log.Warning(exception, "The single-instance lock could not be created; continuing without it");
        return NoLock.Instance;
    }

    private sealed class NoLock : IDisposable
    {
        public static readonly NoLock Instance = new();

        public void Dispose()
        {
        }
    }
}
