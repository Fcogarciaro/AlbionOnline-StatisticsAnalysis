using StatisticsAnalysisTool.Headless.Contract;
using StatisticsAnalysisTool.Headless.Mapping;
using StatisticsAnalysisTool.Headless.Events;

namespace StatisticsAnalysisTool.Headless.Tests;

public class EventMapperTests
{
    private const long LocalId = 42;
    private const string LocalName = "Bob";

    private static (EventMapper mapper, RecordingSink sink) Create(IItemCatalog items = null, IWorldCatalog world = null)
    {
        var sink = new RecordingSink();
        var mapper = new EventMapper(sink, items ?? ItemCatalog.Empty, world ?? WorldCatalog.Empty, NullValueEstimator.Instance);
        return (mapper, sink);
    }

    private static (EventMapper mapper, RecordingSink sink) Joined(IItemCatalog items = null, IWorldCatalog world = null)
    {
        var (mapper, sink) = Create(items, world);
        mapper.OnJoin(new JoinInfo(Wire.Join(LocalId, LocalName)));
        return (mapper, sink);
    }

    [Fact]
    public void Join_EmitsPlayerName()
    {
        var (_, sink) = Joined();

        Assert.Equal(LocalName, sink.Single("player").GetProperty("name").GetString());
    }

    [Fact]
    public void Join_WithMapIndex_EmitsClusterUsingWorldData()
    {
        var world = WorldCatalog.FromEntries([new WorldEntry("3005", "Bridgewatch", "SAFEAREA")]);
        var (mapper, sink) = Create(world: world);

        mapper.OnJoin(new JoinInfo(Wire.Join(LocalId, LocalName, "3005")));

        var cluster = sink.Single("cluster");
        Assert.Equal("3005", cluster.GetProperty("index").GetString());
        Assert.Equal("Bridgewatch", cluster.GetProperty("name").GetString());
        Assert.Equal("safe", cluster.GetProperty("mode").GetString());
    }

    [Theory]
    [InlineData("OPENPVP_YELLOW_1", "yellow")]
    [InlineData("OPENPVP_RED_2", "red")]
    [InlineData("OPENPVP_BLACK_3", "black")]
    [InlineData("PLAYERCITY_BLUE", "blue")]
    [InlineData("SAFEAREA", "safe")]
    [InlineData("HIDEOUT", "safe")]
    [InlineData("SOMETHING_ELSE", "unknown")]
    [InlineData(null, "unknown")]
    public void ClusterMode_IsDerivedFromWorldType(string worldType, string expected)
    {
        Assert.Equal(expected, ClusterModes.FromWorldType(worldType));
    }

    [Fact]
    public void ChangeCluster_WithoutWorldData_IsUnknownAndKeepsIndexAsName()
    {
        var (mapper, sink) = Create();

        mapper.OnChangeCluster(new ChangeClusterInfo(new Dictionary<byte, object> { [0] = "2004" }));

        var cluster = sink.Single("cluster");
        Assert.Equal("2004", cluster.GetProperty("index").GetString());
        Assert.Equal("2004", cluster.GetProperty("name").GetString());
        Assert.Equal("unknown", cluster.GetProperty("mode").GetString());
    }

    [Fact]
    public void ChangeCluster_WithWorldData_ResolvesNameAndMode()
    {
        var world = WorldCatalog.FromEntries([new WorldEntry("2004", "Forest Cross", "OPENPVP_BLACK_1")]);
        var (mapper, sink) = Create(world: world);

        mapper.OnChangeCluster(new ChangeClusterInfo(new Dictionary<byte, object> { [0] = "2004" }));

        var cluster = sink.Single("cluster");
        Assert.Equal("Forest Cross", cluster.GetProperty("name").GetString());
        Assert.Equal("black", cluster.GetProperty("mode").GetString());
    }

    [Fact]
    public void ChangeCluster_IslandIndex_IsSafe()
    {
        var (mapper, sink) = Create();

        mapper.OnChangeCluster(new ChangeClusterInfo(new Dictionary<byte, object> { [0] = "@ISLAND@c640e642-5135-4203-89b5-0007e4215605" }));

        Assert.Equal("safe", sink.Single("cluster").GetProperty("mode").GetString());
    }

    [Fact]
    public void Fame_DecodesFixpointAndSplitsPremiumAndSatchel()
    {
        var (mapper, sink) = Create();

        mapper.OnFame(new FameInfo(new Dictionary<byte, object>
        {
            [1] = Wire.Fix(500000),
            [2] = Wire.Fix(10000),
            [5] = true,
            [10] = Wire.Fix(2000)
        }));

        var fame = sink.Single("fame");
        Assert.Equal(17000.0, fame.GetProperty("amount").GetDouble());
        Assert.Equal(5000.0, fame.GetProperty("premium").GetDouble());
        Assert.Equal(2000.0, fame.GetProperty("satchel").GetDouble());
    }

    [Fact]
    public void Fame_WithoutPremium_HasZeroPremium()
    {
        var (mapper, sink) = Create();

        mapper.OnFame(new FameInfo(new Dictionary<byte, object> { [2] = Wire.Fix(1234.5) }));

        var fame = sink.Single("fame");
        Assert.Equal(1234.5, fame.GetProperty("amount").GetDouble());
        Assert.Equal(0.0, fame.GetProperty("premium").GetDouble());
        Assert.Equal(0.0, fame.GetProperty("satchel").GetDouble());
    }

    [Fact]
    public void Silver_ForLocalPlayer_UsesYieldAfterTax()
    {
        var (mapper, sink) = Joined();

        mapper.OnTakeSilver(new TakeSilverInfo(new Dictionary<byte, object>
        {
            [0] = LocalId,
            [2] = LocalId,
            [3] = Wire.Fix(1000),
            [4] = Wire.Fix(100),
            [5] = Wire.Fix(50)
        }));

        Assert.Equal(850.0, sink.Single("silver").GetProperty("amount").GetDouble());
    }

    [Fact]
    public void Silver_ForAnotherPlayer_IsIgnored()
    {
        var (mapper, sink) = Joined();

        mapper.OnTakeSilver(new TakeSilverInfo(new Dictionary<byte, object> { [0] = 99L, [2] = 5L, [3] = Wire.Fix(1000) }));

        Assert.Empty(sink.Json("silver"));
    }

    [Fact]
    public void Silver_BeforeJoin_IsIgnored()
    {
        var (mapper, sink) = Create();

        mapper.OnTakeSilver(new TakeSilverInfo(new Dictionary<byte, object> { [0] = LocalId, [3] = Wire.Fix(1000) }));

        Assert.Empty(sink.Json("silver"));
    }

    [Fact]
    public void Respec_EmitsGainedPoints()
    {
        var (mapper, sink) = Create();

        mapper.OnReSpecPoints(new ReSpecInfo(new Dictionary<byte, object> { [2] = Wire.Fix(994.8534) }));

        Assert.Equal(994.8534, sink.Single("respec").GetProperty("amount").GetDouble(), 4);
    }

    [Fact]
    public void MightFavor_DecodesBothValues()
    {
        var (mapper, sink) = Create();

        mapper.OnMightAndFavor(new MightFavorInfo(new Dictionary<byte, object> { [1] = Wire.Fix(6.3), [4] = Wire.Fix(1.7) }));

        var line = sink.Single("might_favor");
        Assert.Equal(6.3, line.GetProperty("might").GetDouble(), 4);
        Assert.Equal(1.7, line.GetProperty("favor").GetDouble(), 4);
    }

    [Fact]
    public void Died_OfLocalPlayer_EmitsKilledBy()
    {
        var (mapper, sink) = Joined();

        mapper.OnDied(new DiedInfo(new Dictionary<byte, object> { [1] = LocalId, [2] = LocalName, [10] = "Villain" }));

        Assert.Equal("Villain", sink.Single("died").GetProperty("killedBy").GetString());
    }

    [Fact]
    public void Died_OfAnotherPlayer_IsIgnored()
    {
        var (mapper, sink) = Joined();

        mapper.OnDied(new DiedInfo(new Dictionary<byte, object> { [1] = 77L, [2] = "Alice", [10] = "Villain" }));

        Assert.Empty(sink.Json("died"));
    }

    [Fact]
    public void NewLootChest_EmitsClosedChestWithRarity()
    {
        var (mapper, sink) = Create();

        mapper.OnNewLootChest(new NewLootChestInfo(new Dictionary<byte, object> { [0] = 9, [3] = "TREASURE_SOLO_RARE", [21] = 2 }));

        var chest = sink.Single("chest");
        Assert.Equal("rare", chest.GetProperty("rarity").GetString());
        Assert.False(chest.GetProperty("opened").GetBoolean());
    }

    [Fact]
    public void UpdateLootChest_Opened_ReusesRarityRememberedFromNewLootChest()
    {
        var (mapper, sink) = Create();
        mapper.OnNewLootChest(new NewLootChestInfo(new Dictionary<byte, object> { [0] = 9, [21] = 3 }));

        mapper.OnUpdateLootChest(new UpdateLootChestInfo(new Dictionary<byte, object> { [0] = 9, [1] = 7 }));

        var opened = sink.Json("chest").Last();
        Assert.True(opened.GetProperty("opened").GetBoolean());
        Assert.Equal("legendary", opened.GetProperty("rarity").GetString());
    }

    [Fact]
    public void UpdateLootChest_UnknownRarity_IsUnknown()
    {
        var (mapper, sink) = Create();

        mapper.OnUpdateLootChest(new UpdateLootChestInfo(new Dictionary<byte, object> { [0] = 11, [1] = 7 }));

        Assert.Equal("unknown", sink.Single("chest").GetProperty("rarity").GetString());
    }

    private static GrabbedLootInfo Grab(string from, string by, int itemIndex, int quantity, bool silver = false) =>
        new(new Dictionary<byte, object> { [1] = from, [2] = by, [3] = silver, [4] = itemIndex, [5] = quantity });

    [Fact]
    public void Loot_ByLocalPlayer_ResolvesItemAndMobSource()
    {
        var items = ItemCatalog.FromEntries([new ItemEntry(1841, "T4_BAG", "Adept's Bag")]);
        var (mapper, sink) = Joined(items);

        mapper.OnGrabbedLoot(Grab("T4_MOB_X@MOB@abc", LocalName, 1841, 2));

        var loot = sink.Single("loot");
        Assert.Equal(1841, loot.GetProperty("itemIndex").GetInt32());
        Assert.Equal("T4_BAG", loot.GetProperty("uniqueName").GetString());
        Assert.Equal("Adept's Bag", loot.GetProperty("name").GetString());
        Assert.Equal(2, loot.GetProperty("quantity").GetInt32());
        Assert.Equal("mob", loot.GetProperty("source").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, loot.GetProperty("estValue").ValueKind);
    }

    [Fact]
    public void Loot_FromPlayerBody_IsPlayerSource()
    {
        var (mapper, sink) = Joined();

        mapper.OnGrabbedLoot(Grab("Alice", LocalName, 5, 1));

        Assert.Equal("player", sink.Single("loot").GetProperty("source").GetString());
    }

    [Fact]
    public void Loot_FromChest_IsChestSource()
    {
        var (mapper, sink) = Joined();

        mapper.OnGrabbedLoot(Grab("FOREST_GREEN_LOOTCHEST_X", LocalName, 5, 1));

        Assert.Equal("chest", sink.Single("loot").GetProperty("source").GetString());
    }

    [Fact]
    public void Loot_WithoutSourceOrCatalog_IsUnknownWithNullNames()
    {
        var (mapper, sink) = Joined();

        mapper.OnGrabbedLoot(Grab(null, LocalName, 5, 1));

        var loot = sink.Single("loot");
        Assert.Equal("unknown", loot.GetProperty("source").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, loot.GetProperty("uniqueName").ValueKind);
    }

    [Fact]
    public void Loot_ByAnotherPlayer_IsIgnored()
    {
        var (mapper, sink) = Joined();

        mapper.OnGrabbedLoot(Grab("T4_MOB@MOB", "Alice", 5, 1));

        Assert.Empty(sink.Json("loot"));
    }

    [Fact]
    public void Loot_SilverGrab_IsNotAnItemLoot()
    {
        var (mapper, sink) = Joined();

        mapper.OnGrabbedLoot(Grab("T4_MOB@MOB", LocalName, 0, 1550115, silver: true));

        Assert.Empty(sink.Json("loot"));
    }

    [Fact]
    public void Loot_UsesValueEstimatorWhenAvailable()
    {
        var sink = new RecordingSink();
        var mapper = new EventMapper(sink, ItemCatalog.Empty, WorldCatalog.Empty, new FixedEstimator(1234.5));
        mapper.OnJoin(new JoinInfo(Wire.Join(LocalId, LocalName)));

        mapper.OnGrabbedLoot(Grab("T4_MOB@MOB", LocalName, 5, 2));

        Assert.Equal(1234.5, sink.Single("loot").GetProperty("estValue").GetDouble());
    }

    private sealed class FixedEstimator(double value) : IValueEstimator
    {
        public double? Estimate(int itemIndex, string uniqueName, int quantity) => value;
    }
}
