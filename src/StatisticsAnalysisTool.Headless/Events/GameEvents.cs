using System;
using System.Collections.Generic;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Headless.Contract;

// Photon payload parsers. Each class has the public Dictionary<byte, object> constructor that EventPacketHandler<T> and
// ResponsePacketHandler<T> need. They read the same parameter indexes as the desktop tool's Network/Events and
// Network/Operations classes, without their WPF/controller dependencies.
namespace StatisticsAnalysisTool.Headless.Events;

/// <summary>Join response (operation Join): the local player. Params: 0 object id, 2 name, 8 map index.</summary>
public sealed class JoinInfo
{
    public long? UserObjectId { get; }
    public string Username { get; }
    public string MapIndex { get; }

    public JoinInfo(Dictionary<byte, object> parameters)
    {
        UserObjectId = parameters.TryGetValue(0, out var id) ? WireValue.ToLong(id) : null;
        Username = WireValue.ToText(parameters, 2);
        MapIndex = WireValue.ToText(parameters, 8);
    }
}

/// <summary>ChangeCluster response. Param 0 is the cluster index (or an "@ISLAND@guid" style map string).</summary>
public sealed class ChangeClusterInfo
{
    public string Index { get; }

    public ChangeClusterInfo(Dictionary<byte, object> parameters)
    {
        Index = WireValue.ToText(parameters, 0);
    }
}

/// <summary>UpdateFame event. Params: 2 fame with zone multiplier, 5 premium flag, 10 satchel, 17 bonus factor.</summary>
public sealed class FameInfo
{
    public double Zone { get; }
    public double Premium { get; }
    public double Satchel { get; }
    public double Total { get; }

    public FameInfo(Dictionary<byte, object> parameters)
    {
        var zone = parameters.TryGetValue(2, out var zoneValue) ? FixPoint.FromInternalValue(WireValue.ToLong(zoneValue) ?? 0).DoubleValue : 0;
        var isPremium = parameters.TryGetValue(5, out var premiumValue) && WireValue.ToBool(premiumValue);
        var satchel = parameters.TryGetValue(10, out var satchelValue) ? FixPoint.FromInternalValue(WireValue.ToLong(satchelValue) ?? 0).DoubleValue : 0;

        var bonusFactor = 1.0;
        if (parameters.TryGetValue(17, out var bonusValue))
        {
            var percent = (bonusValue as float? ?? 0) * 100.0;
            // Same rule as the desktop tool: an active bonus factor is reported through the zone fame, not multiplied again.
            bonusFactor = percent > 0 ? 1 : 1 + (bonusValue as float? ?? 0);
        }

        Zone = zone;
        Premium = isPremium && zone > 0 ? zone * 1.5 - zone : 0;
        Satchel = satchel;
        Total = (zone + Premium + satchel) * bonusFactor;
    }
}

/// <summary>TakeSilver event. Params: 0 object id, 2 target id, 3 pre-tax, 4 cluster tax, 5 guild tax, 6 alliance penalty.</summary>
public sealed class TakeSilverInfo
{
    public long? ObjectId { get; }
    public long? TargetEntityId { get; }
    public double YieldAfterTax { get; }

    public TakeSilverInfo(Dictionary<byte, object> parameters)
    {
        ObjectId = parameters.TryGetValue(0, out var objectId) ? WireValue.ToLong(objectId) : null;
        TargetEntityId = parameters.TryGetValue(2, out var target) ? WireValue.ToLong(target) : null;

        var preTax = WireValue.Long(parameters, 3);
        var taxes = WireValue.Long(parameters, 4) + WireValue.Long(parameters, 5) + WireValue.Long(parameters, 6);
        YieldAfterTax = FixPoint.FromInternalValue(Math.Max(0, preTax - taxes)).DoubleValue;
    }
}

/// <summary>UpdateReSpecPoints event. Param 2 is the gained points (shown in game as Fame Credits).</summary>
public sealed class ReSpecInfo
{
    public double Gained { get; }

    public ReSpecInfo(Dictionary<byte, object> parameters)
    {
        Gained = FixPoint.FromInternalValue(WireValue.Long(parameters, 2)).DoubleValue;
    }
}

/// <summary>MightAndFavorReceived event. Params: 1 might, 4 favor.</summary>
public sealed class MightFavorInfo
{
    public double Might { get; }
    public double Favor { get; }

    public MightFavorInfo(Dictionary<byte, object> parameters)
    {
        Might = FixPoint.FromInternalValue(WireValue.Long(parameters, 1)).DoubleValue;
        Favor = FixPoint.FromInternalValue(WireValue.Long(parameters, 4)).DoubleValue;
    }
}

/// <summary>Died event. Params: 1 died object id, 2 died name, 10 killed by.</summary>
public sealed class DiedInfo
{
    public long DiedObjectId { get; }
    public string Died { get; }
    public string KilledBy { get; }

    public DiedInfo(Dictionary<byte, object> parameters)
    {
        DiedObjectId = WireValue.Long(parameters, 1);
        Died = WireValue.ToText(parameters, 2) ?? string.Empty;
        KilledBy = WireValue.ToText(parameters, 10) ?? string.Empty;
    }
}

internal static class RarityParser
{
    public static string FromParameter(Dictionary<byte, object> parameters, byte key)
    {
        if (!parameters.TryGetValue(key, out var value) || value == null || !int.TryParse(value.ToString(), out var rarity))
        {
            return Rarities.Unknown;
        }

        return rarity switch
        {
            0 => Rarities.Common,
            1 => Rarities.Uncommon,
            2 => Rarities.Rare,
            3 => Rarities.Legendary,
            _ => Rarities.Unknown
        };
    }
}

/// <summary>NewLootChest event. Params: 0 object id, 3 unique name, 21 rarity (23 for STATIC_ chests).</summary>
public sealed class NewLootChestInfo
{
    public int ObjectId { get; }
    public string Rarity { get; }

    public NewLootChestInfo(Dictionary<byte, object> parameters)
    {
        ObjectId = parameters.TryGetValue(0, out var id) && int.TryParse(id?.ToString(), out var parsed) ? parsed : 0;
        var uniqueName = WireValue.ToText(parameters, 3);
        var rarity = RarityParser.FromParameter(parameters, 21);
        if (rarity == Rarities.Unknown && uniqueName?.StartsWith("STATIC_", StringComparison.Ordinal) == true)
        {
            rarity = RarityParser.FromParameter(parameters, 23);
        }

        Rarity = rarity;
    }
}

/// <summary>UpdateLootChest event. Params: 0 object id, 1 state (7 = opened), 3 player guids, 13 rarity.</summary>
public sealed class UpdateLootChestInfo
{
    private const int OpenedState = 7;

    public int ObjectId { get; }
    public bool IsOpened { get; }
    public string Rarity { get; }

    public UpdateLootChestInfo(Dictionary<byte, object> parameters)
    {
        ObjectId = parameters.TryGetValue(0, out var id) && int.TryParse(id?.ToString(), out var parsed) ? parsed : 0;
        if (parameters.TryGetValue(1, out var state))
        {
            IsOpened = int.TryParse(state?.ToString(), out var parsedState) && parsedState == OpenedState;
        }
        else
        {
            IsOpened = parameters.ContainsKey(3);
        }

        Rarity = RarityParser.FromParameter(parameters, 13);
    }
}

/// <summary>OtherGrabbedLoot event. Params: 1 looted body, 2 looter, 3 is silver, 4 item index, 5 quantity.</summary>
public sealed class GrabbedLootInfo
{
    public string LootedFrom { get; }
    public string LootedBy { get; }
    public bool IsSilver { get; }
    public int ItemIndex { get; }
    public int Quantity { get; }

    public GrabbedLootInfo(Dictionary<byte, object> parameters)
    {
        LootedFrom = WireValue.ToText(parameters, 1);
        LootedBy = WireValue.ToText(parameters, 2);
        IsSilver = parameters.TryGetValue(3, out var silver) && WireValue.ToBool(silver);
        ItemIndex = parameters.TryGetValue(4, out var item) ? WireValue.ToInt(item) : 0;
        Quantity = parameters.TryGetValue(5, out var quantity) ? WireValue.ToInt(quantity) : 0;
    }
}
