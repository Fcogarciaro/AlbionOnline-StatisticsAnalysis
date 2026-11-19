using System.Text.Json;
using StatisticsAnalysisTool.Headless.Contract;
using StatisticsAnalysisTool.Headless.Mapping;

namespace StatisticsAnalysisTool.Headless.Tests;

/// <summary>Captures what the mapper emits as the exact JSON lines a consumer would read.</summary>
internal sealed class RecordingSink : IEventSink
{
    public static readonly DateTimeOffset FixedTime = new(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero);

    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public void Emit(ContractMessage message) => _lines.Add(JsonLineSerializer.Serialize(message, FixedTime));

    public JsonElement Single(string type)
    {
        var matches = Json(type);
        Assert.Single(matches);
        return matches[0];
    }

    public List<JsonElement> Json(string type) => _lines
        .Select(line => JsonDocument.Parse(line).RootElement.Clone())
        .Where(e => e.GetProperty("type").GetString() == type)
        .ToList();
}

internal static class Wire
{
    /// <summary>Internal fixpoint value of a decimal amount (factor 10000).</summary>
    public static long Fix(double value) => (long) Math.Round(value * 10000);

    public static Dictionary<byte, object> Join(long objectId, string name, string mapIndex = null)
    {
        var p = new Dictionary<byte, object> { [0] = objectId, [2] = name };
        if (mapIndex != null)
        {
            p[8] = mapIndex;
        }

        return p;
    }
}
