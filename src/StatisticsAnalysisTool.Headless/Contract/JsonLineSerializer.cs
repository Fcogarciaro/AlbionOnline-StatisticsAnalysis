using System;
using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace StatisticsAnalysisTool.Headless.Contract;

public static class JsonLineSerializer
{
    public const int ContractVersion = 1;

    public static string Serialize(ContractMessage message, DateTimeOffset timestamp)
    {
        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("v", ContractVersion);
            writer.WriteString("type", message.Type);
            writer.WriteString("ts", timestamp.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));
            message.WriteFields(writer);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
