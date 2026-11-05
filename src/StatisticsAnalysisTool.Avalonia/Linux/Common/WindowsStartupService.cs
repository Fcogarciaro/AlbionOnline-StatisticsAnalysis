// Linux replacement for Common/WindowsStartupService.cs (HKCU Run registry key).
// TODO(T18): write/remove an XDG autostart .desktop entry.
namespace StatisticsAnalysisTool.Common;

public static class WindowsStartupService
{
    /// <summary>Disabling is trivially successful; enabling is not supported yet and reports failure.</summary>
    public static bool TrySetEnabled(bool isEnabled) => !isEnabled;
}
