using System;
using System.Collections.Generic;
using StatisticsAnalysisTool.Headless.Contract;
using StatisticsAnalysisTool.Headless.Events;

namespace StatisticsAnalysisTool.Headless.Mapping;

/// <summary>
/// Turns parsed game events into contract lines. Holds the local-player identity (from the Join response) so that
/// silver, loot and death lines are only produced for the local player. Independent of capture and of the Photon layer.
/// </summary>
public sealed class EventMapper
{
    private readonly IEventSink _sink;
    private readonly IItemCatalog _items;
    private readonly IWorldCatalog _world;
    private readonly IValueEstimator _estimator;
    private readonly object _lock = new();
    private readonly Dictionary<int, string> _chestRarity = new();
    private readonly Dictionary<int, bool> _chestOpened = new();
    private long? _localObjectId;
    private string _localName;

    public EventMapper(IEventSink sink, IItemCatalog items, IWorldCatalog world, IValueEstimator estimator)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _items = items ?? ItemCatalog.Empty;
        _world = world ?? WorldCatalog.Empty;
        _estimator = estimator ?? NullValueEstimator.Instance;
    }

    public void OnJoin(JoinInfo join)
    {
        lock (_lock)
        {
            _localObjectId = join.UserObjectId;
            _localName = string.IsNullOrEmpty(join.Username) ? null : join.Username;
            _chestRarity.Clear();
            _chestOpened.Clear();

            if (_localName != null)
            {
                _sink.Emit(new PlayerMessage(_localName));
            }

            if (!string.IsNullOrEmpty(join.MapIndex))
            {
                EmitCluster(join.MapIndex);
            }
        }
    }

    public void OnChangeCluster(ChangeClusterInfo change)
    {
        if (string.IsNullOrEmpty(change.Index))
        {
            return;
        }

        lock (_lock)
        {
            _chestRarity.Clear();
            _chestOpened.Clear();
            EmitCluster(change.Index);
        }
    }

    private void EmitCluster(string index)
    {
        if (_world.TryGet(index, out var entry))
        {
            _sink.Emit(new ClusterMessage(index, string.IsNullOrEmpty(entry.Name) ? index : entry.Name, ClusterModes.FromWorldType(entry.Type)));
            return;
        }

        // Islands and hideouts arrive as "@ISLAND@guid" / "@HIDEOUT@id@guid" strings and are always safe areas.
        var isSafeMap = index.StartsWith("@ISLAND", StringComparison.OrdinalIgnoreCase) || index.StartsWith("@HIDEOUT", StringComparison.OrdinalIgnoreCase);
        _sink.Emit(new ClusterMessage(index, index, isSafeMap ? ClusterModeNames.Safe : ClusterModeNames.Unknown));
    }

    public void OnFame(FameInfo fame) => _sink.Emit(new FameMessage(fame.Total, fame.Premium, fame.Satchel));

    public void OnTakeSilver(TakeSilverInfo silver)
    {
        lock (_lock)
        {
            // Party members' shares are deliberately not counted; only the local player's own take is reported.
            if (_localObjectId == null || silver.ObjectId != _localObjectId)
            {
                return;
            }
        }

        _sink.Emit(new SilverMessage(silver.YieldAfterTax));
    }

    public void OnReSpecPoints(ReSpecInfo respec) => _sink.Emit(new RespecMessage(respec.Gained));

    public void OnMightAndFavor(MightFavorInfo value) => _sink.Emit(new MightFavorMessage(value.Might, value.Favor));

    public void OnDied(DiedInfo died)
    {
        lock (_lock)
        {
            var isLocalById = _localObjectId != null && died.DiedObjectId == _localObjectId;
            var isLocalByName = _localName != null && string.Equals(died.Died, _localName, StringComparison.Ordinal);
            if (!isLocalById && !isLocalByName)
            {
                return;
            }
        }

        _sink.Emit(new DiedMessage(died.KilledBy));
    }

    public void OnNewLootChest(NewLootChestInfo chest)
    {
        lock (_lock)
        {
            _chestRarity[chest.ObjectId] = chest.Rarity;
            _chestOpened[chest.ObjectId] = false;
        }

        _sink.Emit(new ChestMessage(chest.Rarity, false));
    }

    public void OnUpdateLootChest(UpdateLootChestInfo chest)
    {
        string rarity;
        lock (_lock)
        {
            // Skip repeated updates that do not change the opened state of a chest already reported.
            if (_chestOpened.TryGetValue(chest.ObjectId, out var known) && known == chest.IsOpened)
            {
                return;
            }

            rarity = chest.Rarity;
            if (rarity == Rarities.Unknown && _chestRarity.TryGetValue(chest.ObjectId, out var remembered))
            {
                rarity = remembered;
            }

            _chestRarity[chest.ObjectId] = rarity;
            _chestOpened[chest.ObjectId] = chest.IsOpened;
        }

        _sink.Emit(new ChestMessage(rarity, chest.IsOpened));
    }

    public void OnGrabbedLoot(GrabbedLootInfo loot)
    {
        lock (_lock)
        {
            if (loot.IsSilver || _localName == null || !string.Equals(loot.LootedBy, _localName, StringComparison.Ordinal))
            {
                return;
            }
        }

        _items.TryGet(loot.ItemIndex, out var item);
        _sink.Emit(new LootMessage(loot.ItemIndex, item?.UniqueName, item?.Name, loot.Quantity, ClassifySource(loot.LootedFrom),
            _estimator.Estimate(loot.ItemIndex, item?.UniqueName, loot.Quantity)));
    }

    /// <summary>
    /// The looted body is named after the mob (unique name containing "@MOB") or after the player it belonged to.
    /// Chest bodies carry "CHEST" in their name. Anything else (including a missing name) is unknown.
    /// </summary>
    internal static string ClassifySource(string lootedFrom)
    {
        if (string.IsNullOrEmpty(lootedFrom))
        {
            return LootSources.Unknown;
        }

        if (lootedFrom.Contains("@MOB", StringComparison.Ordinal))
        {
            return LootSources.Mob;
        }

        if (lootedFrom.Contains("CHEST", StringComparison.Ordinal))
        {
            return LootSources.Chest;
        }

        return LootSources.Player;
    }
}
