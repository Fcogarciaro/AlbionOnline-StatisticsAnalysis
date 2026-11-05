// Placeholders for the WPF windows referenced by linked ViewModels/controllers. Each is replaced by the real
// Avalonia view of the same name and namespace in T09/T10 (delete the placeholder when the view lands).
using StatisticsAnalysisTool.Models;
using System.Windows;

namespace StatisticsAnalysisTool.Views;

/// <summary>
/// Placeholder for the first start guide (T10). Like the WPF guide it is shown modally by <c>App</c> startup and
/// returns true when completed; this one completes immediately without collecting anything and does not persist
/// <c>HasCompletedFirstStartGuide</c>, so it is shown again on the next start.
/// </summary>
public class FirstStartGuideWindow : Window
{
    public new bool? ShowDialog()
    {
        Serilog.Log.Warning("The first start guide is not ported yet (T10); continuing without it");
        return true;
    }
}
