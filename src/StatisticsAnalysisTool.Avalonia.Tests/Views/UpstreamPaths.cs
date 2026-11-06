namespace StatisticsAnalysisTool.Avalonia.Tests.Views;

internal static class UpstreamPaths
{
    /// <summary>Path of a file below the upstream WPF project (src/StatisticsAnalysisTool).</summary>
    public static string File(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "StatisticsAnalysisTool")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir!.FullName, "StatisticsAnalysisTool", relative);
    }
}
