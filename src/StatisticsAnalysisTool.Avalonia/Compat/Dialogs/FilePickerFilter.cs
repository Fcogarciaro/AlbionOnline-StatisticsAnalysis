// Pure helpers translating WPF/Win32 file dialog conventions (filter strings, DefaultExt) to picker inputs.
using System;
using System.Collections.Generic;

namespace Microsoft.Win32;

internal readonly record struct FilterEntry(string Name, IReadOnlyList<string> Patterns);

internal static class FilePickerFilter
{
    /// <summary>Parses "Name (.csv)|*.csv|All files|*.*" (patterns separated by ';') into entries.</summary>
    public static IReadOnlyList<FilterEntry> Parse(string filter)
    {
        var entries = new List<FilterEntry>();
        if (string.IsNullOrWhiteSpace(filter))
        {
            return entries;
        }

        var parts = filter.Split('|');
        for (var i = 0; i + 1 < parts.Length; i += 2)
        {
            var patterns = parts[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (patterns.Length > 0)
            {
                entries.Add(new FilterEntry(parts[i].Trim(), patterns));
            }
        }

        return entries;
    }

    /// <summary>Win32 AddExtension: appends DefaultExt (with or without leading dot) when the name has no extension.</summary>
    public static string EnsureExtension(string fileName, string defaultExt)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(defaultExt) || System.IO.Path.HasExtension(fileName))
        {
            return fileName;
        }

        return defaultExt.StartsWith('.') ? fileName + defaultExt : $"{fileName}.{defaultExt}";
    }
}
