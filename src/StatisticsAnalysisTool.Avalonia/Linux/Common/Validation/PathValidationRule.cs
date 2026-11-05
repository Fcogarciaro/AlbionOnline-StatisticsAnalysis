// Linux replacement for Common/Validation/PathValidationRule.cs (a WPF ValidationRule). Same checks and messages; the
// settings view shows the message as the tooltip and the "invalid" class of the text box (see SettingsControl).
using System.IO;

namespace StatisticsAnalysisTool.Common.Validation;

public static class PathValidationRule
{
    /// <summary>The validation message, or null when the path is an existing directory.</summary>
    public static string GetError(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Path cannot be empty.";
        }

        if (!Directory.Exists(path))
        {
            return "Path does not exist.";
        }

        return null;
    }
}
