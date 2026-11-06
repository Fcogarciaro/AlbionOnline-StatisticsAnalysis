// Subset of MahApps.Metro.IconPacks.PackIconGameIconsKind (MahApps.Metro.IconPacks.GameIcons 6.2.1, MIT): only the kinds that
// the upstream XAML uses. Same namespace and names, so ported views keep `Kind="Info"`. To add a kind, add it here and in
// Linux/Icons/GameIconsData.g.cs (path data from the package's embedded Icons.json).
namespace MahApps.Metro.IconPacks;

public enum PackIconGameIconsKind
{
    None = 0,
    Awareness = 1,
    Broadsword = 2,
    BulletImpacts = 3,
    CrossedSwords = 4,
    CrystalWand = 5,
    GooeyImpact = 6,
    Healing = 7,
    Hourglass = 8,
    Info = 9,
    ShieldDisabled = 10,
    SkullCrack = 11,
    SnakeBite = 12,
}
