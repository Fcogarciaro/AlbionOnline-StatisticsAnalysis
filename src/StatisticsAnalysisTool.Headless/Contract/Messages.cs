using System.Text.Json;

namespace StatisticsAnalysisTool.Headless.Contract;

public static class StatusStates
{
    public const string Capturing = "capturing";
    public const string NoPrivilege = "no_privilege";
    public const string Error = "error";
    public const string Stopped = "stopped";
}

public static class ClusterModeNames
{
    public const string Black = "black";
    public const string Red = "red";
    public const string Yellow = "yellow";
    public const string Blue = "blue";
    public const string Safe = "safe";
    public const string Unknown = "unknown";
}

public static class LootSources
{
    public const string Chest = "chest";
    public const string Mob = "mob";
    public const string Player = "player";
    public const string Unknown = "unknown";
}

public static class Rarities
{
    public const string Common = "common";
    public const string Uncommon = "uncommon";
    public const string Rare = "rare";
    public const string Legendary = "legendary";
    public const string Unknown = "unknown";
}

/// <summary>One line of the contract v1 output. The common fields (v, type, ts) are written by the serializer.</summary>
public abstract record ContractMessage(string Type)
{
    public abstract void WriteFields(Utf8JsonWriter writer);
}

public sealed record StatusMessage(string State, string Message) : ContractMessage("status")
{
    public override void WriteFields(Utf8JsonWriter writer)
    {
        writer.WriteString("state", State);
        writer.WriteString("message", Message);
    }
}

public sealed record PlayerMessage(string Name) : ContractMessage("player")
{
    public override void WriteFields(Utf8JsonWriter writer) => writer.WriteString("name", Name);
}

public sealed record ClusterMessage(string Index, string Name, string Mode) : ContractMessage("cluster")
{
    public override void WriteFields(Utf8JsonWriter writer)
    {
        writer.WriteString("index", Index);
        writer.WriteString("name", Name);
        writer.WriteString("mode", Mode);
    }
}

public sealed record FameMessage(double Amount, double Premium, double Satchel) : ContractMessage("fame")
{
    public override void WriteFields(Utf8JsonWriter writer)
    {
        writer.WriteNumber("amount", Amount);
        writer.WriteNumber("premium", Premium);
        writer.WriteNumber("satchel", Satchel);
    }
}

public sealed record SilverMessage(double Amount) : ContractMessage("silver")
{
    public override void WriteFields(Utf8JsonWriter writer) => writer.WriteNumber("amount", Amount);
}

public sealed record RespecMessage(double Amount) : ContractMessage("respec")
{
    public override void WriteFields(Utf8JsonWriter writer) => writer.WriteNumber("amount", Amount);
}

public sealed record MightFavorMessage(double Might, double Favor) : ContractMessage("might_favor")
{
    public override void WriteFields(Utf8JsonWriter writer)
    {
        writer.WriteNumber("might", Might);
        writer.WriteNumber("favor", Favor);
    }
}

public sealed record DiedMessage(string KilledBy) : ContractMessage("died")
{
    public override void WriteFields(Utf8JsonWriter writer) => writer.WriteString("killedBy", KilledBy);
}

public sealed record ChestMessage(string Rarity, bool Opened) : ContractMessage("chest")
{
    public override void WriteFields(Utf8JsonWriter writer)
    {
        writer.WriteString("rarity", Rarity);
        writer.WriteBoolean("opened", Opened);
    }
}

public sealed record LootMessage(int ItemIndex, string UniqueName, string Name, int Quantity, string Source, double? EstValue) : ContractMessage("loot")
{
    public override void WriteFields(Utf8JsonWriter writer)
    {
        writer.WriteNumber("itemIndex", ItemIndex);
        writer.WriteString("uniqueName", UniqueName);
        writer.WriteString("name", Name);
        writer.WriteNumber("quantity", Quantity);
        writer.WriteString("source", Source);
        if (EstValue.HasValue)
        {
            writer.WriteNumber("estValue", EstValue.Value);
        }
        else
        {
            writer.WriteNull("estValue");
        }
    }
}
