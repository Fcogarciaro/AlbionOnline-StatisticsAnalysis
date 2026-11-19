using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using StatisticsAnalysisTool.Headless.Contract;

namespace StatisticsAnalysisTool.Headless.Mapping;

public sealed record ItemEntry(int Index, string UniqueName, string Name);

public sealed record WorldEntry(string Index, string Name, string Type);

public interface IItemCatalog
{
    bool TryGet(int index, out ItemEntry item);
}

public interface IWorldCatalog
{
    bool TryGet(string index, out WorldEntry entry);
}

public interface IValueEstimator
{
    /// <summary>Estimated silver value of the stack, or null when no price data is available.</summary>
    double? Estimate(int itemIndex, string uniqueName, int quantity);
}

public sealed class NullValueEstimator : IValueEstimator
{
    public static readonly NullValueEstimator Instance = new();

    public double? Estimate(int itemIndex, string uniqueName, int quantity) => null;
}

public static class GameFilesLocator
{
    /// <summary>$XDG_DATA_HOME/StatisticsAnalysisTool/GameFiles, the directory the desktop tool extracts game data to.</summary>
    public static string Directory()
    {
        var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrWhiteSpace(dataHome))
        {
            dataHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        return Path.Combine(dataHome, "StatisticsAnalysisTool", "GameFiles");
    }

    public static string FindFile(string directory, string fileName)
    {
        if (!System.IO.Directory.Exists(directory))
        {
            return null;
        }

        return System.IO.Directory.EnumerateFiles(directory)
            .FirstOrDefault(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class ItemCatalog : IItemCatalog
{
    public const string FileName = "IndexedItems.json";

    public static readonly ItemCatalog Empty = new(new Dictionary<int, ItemEntry>());

    private readonly Dictionary<int, ItemEntry> _items;

    private ItemCatalog(Dictionary<int, ItemEntry> items) => _items = items;

    public bool TryGet(int index, out ItemEntry item) => _items.TryGetValue(index, out item);

    public static ItemCatalog FromEntries(IEnumerable<ItemEntry> entries) => new(entries.GroupBy(e => e.Index).ToDictionary(g => g.Key, g => g.First()));

    public static ItemCatalog LoadFrom(string directory)
    {
        var path = GameFilesLocator.FindFile(directory, FileName);
        if (path == null)
        {
            return Empty;
        }

        try
        {
            return FromJson(File.ReadAllText(path));
        }
        catch (IOException)
        {
            return Empty;
        }
        catch (UnauthorizedAccessException)
        {
            return Empty;
        }
    }

    public static ItemCatalog FromJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Empty;
            }

            var entries = new List<ItemEntry>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object || !JsonReading.TryReadInt(element, "Index", out var index))
                {
                    continue;
                }

                string name = null;
                if (element.TryGetProperty("LocalizedNames", out var names) && names.ValueKind == JsonValueKind.Object
                    && names.TryGetProperty("EN-US", out var english) && english.ValueKind == JsonValueKind.String)
                {
                    name = english.GetString();
                }

                entries.Add(new ItemEntry(index, JsonReading.ReadString(element, "UniqueName"), name));
            }

            return FromEntries(entries);
        }
        catch (JsonException)
        {
            return Empty;
        }
    }
}

public sealed class WorldCatalog : IWorldCatalog
{
    public const string FileName = "world.json";

    public static readonly WorldCatalog Empty = new(new Dictionary<string, WorldEntry>());

    private readonly Dictionary<string, WorldEntry> _entries;

    private WorldCatalog(Dictionary<string, WorldEntry> entries) => _entries = entries;

    public bool TryGet(string index, out WorldEntry entry)
    {
        entry = null;
        return index != null && _entries.TryGetValue(index, out entry);
    }

    public static WorldCatalog FromEntries(IEnumerable<WorldEntry> entries) => new(entries.GroupBy(e => e.Index).ToDictionary(g => g.Key, g => g.First()));

    public static WorldCatalog LoadFrom(string directory)
    {
        var path = GameFilesLocator.FindFile(directory, FileName);
        if (path == null)
        {
            return Empty;
        }

        try
        {
            return FromJson(File.ReadAllText(path));
        }
        catch (IOException)
        {
            return Empty;
        }
        catch (UnauthorizedAccessException)
        {
            return Empty;
        }
    }

    /// <summary>Accepts a flat array of clusters or the extractor's {"world":{"clusters":{"cluster":[...]}}} shape.</summary>
    public static WorldCatalog FromJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("world", out var world) && world.TryGetProperty("clusters", out var clusters) && clusters.TryGetProperty("cluster", out var list))
            {
                root = list;
            }

            if (root.ValueKind != JsonValueKind.Array)
            {
                return Empty;
            }

            var entries = new List<WorldEntry>();
            foreach (var element in root.EnumerateArray())
            {
                var id = JsonReading.ReadString(element, "@id");
                if (!string.IsNullOrEmpty(id))
                {
                    entries.Add(new WorldEntry(id, JsonReading.ReadString(element, "@displayname"), JsonReading.ReadString(element, "@type")));
                }
            }

            return FromEntries(entries);
        }
        catch (JsonException)
        {
            return Empty;
        }
    }
}

public static class ClusterModes
{
    /// <summary>Maps the world.json cluster type to the contract mode. Same keywords as the desktop tool, plus blue.</summary>
    public static string FromWorldType(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return ClusterModeNames.Unknown;
        }

        var upper = type.ToUpperInvariant();
        if (upper.Contains("BLUE"))
        {
            return ClusterModeNames.Blue;
        }

        if (upper.Contains("SAFEAREA") || upper == "HIDEOUT" || upper.Contains("PLAYERCITY"))
        {
            return ClusterModeNames.Safe;
        }

        if (upper.Contains("YELLOW"))
        {
            return ClusterModeNames.Yellow;
        }

        if (upper.Contains("RED"))
        {
            return ClusterModeNames.Red;
        }

        if (upper.Contains("BLACK"))
        {
            return ClusterModeNames.Black;
        }

        return ClusterModeNames.Unknown;
    }
}

internal static class JsonReading
{
    public static string ReadString(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    public static bool TryReadInt(JsonElement element, string name, out int result)
    {
        result = 0;
        if (!element.TryGetProperty(name, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt32(out result),
            JsonValueKind.String => int.TryParse(value.GetString(), out result),
            _ => false
        };
    }
}
