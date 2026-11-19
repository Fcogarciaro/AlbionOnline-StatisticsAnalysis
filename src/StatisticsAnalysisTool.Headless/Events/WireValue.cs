using System;
using System.Collections.Generic;

namespace StatisticsAnalysisTool.Headless.Events;

/// <summary>Typed reads of Photon parameters, mirroring the helpers of the desktop tool (ObjectToLong and friends).</summary>
internal static class WireValue
{
    public static long? ToLong(object value) => value switch
    {
        byte v => v,
        sbyte v => v,
        short v => v,
        ushort v => v,
        int v => v,
        uint v => v,
        long v => v,
        _ => null
    };

    public static int ToInt(object value) => value switch
    {
        byte v => v,
        sbyte v => v,
        short v => v,
        ushort v => v,
        int v => v,
        uint v when v <= int.MaxValue => (int) v,
        long v when v is >= int.MinValue and <= int.MaxValue => (int) v,
        _ => 0
    };

    public static bool ToBool(object value) => value as bool? ?? false;

    public static string ToText(Dictionary<byte, object> parameters, byte key)
    {
        return parameters.TryGetValue(key, out var value) && value != null ? value.ToString() : null;
    }

    public static long Long(Dictionary<byte, object> parameters, byte key) =>
        parameters.TryGetValue(key, out var value) ? ToLong(value) ?? 0 : 0;
}
