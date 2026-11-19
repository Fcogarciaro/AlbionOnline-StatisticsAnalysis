using StatisticsAnalysisTool.Headless.Mapping;

namespace StatisticsAnalysisTool.Headless.Tests;

public class CatalogTests
{
    [Fact]
    public void ItemCatalog_ParsesIndexedItemsJson_WithStringNumbers()
    {
        const string json = """
            [
              { "LocalizationNameVariable": "@ITEMS_T4_BAG", "LocalizedNames": { "EN-US": "Adept's Bag", "DE-DE": "Tasche" }, "Index": "1841", "UniqueName": "T4_BAG" },
              { "LocalizedNames": null, "Index": 7, "UniqueName": "T1_X" }
            ]
            """;

        var catalog = ItemCatalog.FromJson(json);

        Assert.True(catalog.TryGet(1841, out var bag));
        Assert.Equal("T4_BAG", bag.UniqueName);
        Assert.Equal("Adept's Bag", bag.Name);
        Assert.True(catalog.TryGet(7, out var other));
        Assert.Null(other.Name);
        Assert.False(catalog.TryGet(9999, out _));
    }

    [Fact]
    public void ItemCatalog_InvalidJson_YieldsEmptyCatalog()
    {
        Assert.False(ItemCatalog.FromJson("not json").TryGet(1, out _));
    }

    [Fact]
    public void ItemCatalog_LoadFrom_MissingDirectory_IsEmpty()
    {
        var catalog = ItemCatalog.LoadFrom(Path.Combine(Path.GetTempPath(), "sat-headless-missing-" + Guid.NewGuid()));

        Assert.False(catalog.TryGet(1, out _));
    }

    [Fact]
    public void ItemCatalog_LoadFrom_ReadsIndexedItemsFileCaseInsensitively()
    {
        var dir = Directory.CreateTempSubdirectory("sat-headless-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(dir, "indexedItems.json"), """[{"Index":3,"UniqueName":"T3_A","LocalizedNames":{"EN-US":"A"}}]""");

            Assert.True(ItemCatalog.LoadFrom(dir).TryGet(3, out var item));
            Assert.Equal("A", item.Name);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void WorldCatalog_ParsesFlatAndRootedShapes()
    {
        const string flat = """[{"@id":"2004","@displayname":"Forest Cross","@type":"OPENPVP_RED_1"}]""";
        const string rooted = """{"world":{"clusters":{"cluster":[{"@id":"3005","@displayname":"Bridgewatch","@type":"SAFEAREA"}]}}}""";

        Assert.True(WorldCatalog.FromJson(flat).TryGet("2004", out var a));
        Assert.Equal("OPENPVP_RED_1", a.Type);
        Assert.True(WorldCatalog.FromJson(rooted).TryGet("3005", out var b));
        Assert.Equal("Bridgewatch", b.Name);
    }

    [Fact]
    public void WorldCatalog_InvalidJson_IsEmpty()
    {
        Assert.False(WorldCatalog.FromJson("{").TryGet("1", out _));
    }
}
