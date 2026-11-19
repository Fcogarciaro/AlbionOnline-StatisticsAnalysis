using System.Text.Json;
using StatisticsAnalysisTool.Headless.Contract;

namespace StatisticsAnalysisTool.Headless.Tests;

public class ContractTests
{
    [Fact]
    public void StatusLine_HasCommonFieldsInOrder()
    {
        var line = JsonLineSerializer.Serialize(new StatusMessage(StatusStates.Capturing, "listening"), RecordingSink.FixedTime);

        Assert.Equal("{\"v\":1,\"type\":\"status\",\"ts\":\"2026-01-02T03:04:05.678Z\",\"state\":\"capturing\",\"message\":\"listening\"}", line);
    }

    [Theory]
    [InlineData(StatusStates.Capturing, "capturing")]
    [InlineData(StatusStates.NoPrivilege, "no_privilege")]
    [InlineData(StatusStates.Error, "error")]
    [InlineData(StatusStates.Stopped, "stopped")]
    public void StatusStates_UseContractSpelling(string state, string expected)
    {
        Assert.Equal(expected, state);
    }

    [Fact]
    public void Timestamp_IsConvertedToUtc()
    {
        var local = new DateTimeOffset(2026, 5, 6, 10, 0, 0, TimeSpan.FromHours(2));

        var line = JsonLineSerializer.Serialize(new PlayerMessage("Bob"), local);

        Assert.Contains("\"ts\":\"2026-05-06T08:00:00.000Z\"", line);
    }

    [Fact]
    public void Line_NeverContainsRawNewlines()
    {
        var line = JsonLineSerializer.Serialize(new StatusMessage(StatusStates.Error, "a\nb\r\nc"), RecordingSink.FixedTime);

        Assert.DoesNotContain('\n', line);
        Assert.DoesNotContain('\r', line);
        Assert.Equal("a\nb\r\nc", JsonDocument.Parse(line).RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public void JsonLineSink_WritesOneLinePerMessageAndFlushes()
    {
        var writer = new StringWriter { NewLine = "\n" };
        var sink = new JsonLineSink(writer, () => RecordingSink.FixedTime);

        sink.Emit(new PlayerMessage("Bob"));
        sink.Emit(new StatusMessage(StatusStates.Stopped, "bye"));

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("\"type\":\"player\"", lines[0]);
        Assert.Contains("\"type\":\"status\"", lines[1]);
    }

    [Fact]
    public void LootLine_NullFieldsAreWrittenAsNull()
    {
        var line = JsonLineSerializer.Serialize(new LootMessage(7, null, null, 3, LootSources.Unknown, null), RecordingSink.FixedTime);

        var root = JsonDocument.Parse(line).RootElement;
        Assert.Equal(JsonValueKind.Null, root.GetProperty("uniqueName").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("name").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("estValue").ValueKind);
        Assert.Equal("unknown", root.GetProperty("source").GetString());
    }
}
