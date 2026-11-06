// Lookup of the game-icons.net path data (see GameIconsData.g.cs) and the parsed geometry for the PackIconGameIcons control.
using System.Collections.Generic;
using Avalonia.Media;
using MahApps.Metro.IconPacks;

namespace StatisticsAnalysisTool.Linux.Icons;

internal static partial class GameIconsData
{
    /// <summary>The icons are drawn in a 512x512 view box.</summary>
    public const double ViewBoxSize = 512;

    private static readonly Dictionary<PackIconGameIconsKind, string> Data = BuildData();
    private static readonly Dictionary<PackIconGameIconsKind, Geometry> Geometries = new();

    public static bool TryGetPathData(PackIconGameIconsKind kind, out string pathData) => Data.TryGetValue(kind, out pathData);

    public static Geometry CreateGeometry(PackIconGameIconsKind kind)
    {
        if (!Data.TryGetValue(kind, out var pathData))
        {
            return null;
        }

        lock (Geometries)
        {
            if (!Geometries.TryGetValue(kind, out var geometry))
            {
                Geometries[kind] = geometry = Geometry.Parse(pathData);
            }

            return geometry;
        }
    }
}
