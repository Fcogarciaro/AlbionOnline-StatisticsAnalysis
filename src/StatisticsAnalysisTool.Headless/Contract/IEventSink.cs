using System;
using System.IO;

namespace StatisticsAnalysisTool.Headless.Contract;

public interface IEventSink
{
    void Emit(ContractMessage message);
}

/// <summary>Writes each message as one JSON line and flushes immediately so consumers see it in real time.</summary>
public sealed class JsonLineSink : IEventSink
{
    private readonly TextWriter _writer;
    private readonly Func<DateTimeOffset> _clock;
    private readonly object _lock = new();

    public JsonLineSink(TextWriter writer, Func<DateTimeOffset> clock = null)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public void Emit(ContractMessage message)
    {
        var line = JsonLineSerializer.Serialize(message, _clock());
        lock (_lock)
        {
            _writer.Write(line);
            _writer.Write('\n');
            _writer.Flush();
        }
    }
}
