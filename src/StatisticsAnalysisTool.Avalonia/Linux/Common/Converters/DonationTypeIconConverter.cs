// DataTrigger list of Donation.Icon.Style (36 DonationType values, each a different Source): one lookup table instead of
// 36 bound classes. The table is checked against the upstream XAML by a test. Unknown values keep the style's default (TOAD1).
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using StatisticsAnalysisTool.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace StatisticsAnalysisTool.Common.Converters;

public sealed class DonationTypeIconConverter : IValueConverter
{
    private const string Folder = "avares://StatisticsAnalysisTool/Resources/DonationIcon/";
    private const string DefaultFile = "TOAD1.png";

    public static readonly DonationTypeIconConverter Instance = new();

    private static readonly ConcurrentDictionary<string, Bitmap> Cache = new(StringComparer.Ordinal);

    private static readonly Dictionary<DonationType, string> Files = new()
    {
        [DonationType.Toad] = "TOAD1.png",
        [DonationType.Marmot] = "MARMOT1.png",
        [DonationType.HillMarmot] = "HILLMARMOT1.png",
        [DonationType.Rabbit] = "RABBIT.png",
        [DonationType.SnowRabbit] = "SNOWRABBIT1.png",
        [DonationType.Snake] = "SNAKE1.png",
        [DonationType.Fox] = "FOX.png",
        [DonationType.Impala] = "IMPALA1.png",
        [DonationType.MoaBird] = "MOABIRD1.png",
        [DonationType.Wolf] = "WOLF.png",
        [DonationType.CritterCougar] = "CRITTERCOUGAR1.png",
        [DonationType.GiantToad] = "GIANTTOAD1.png",
        [DonationType.Boar] = "BOAR.png",
        [DonationType.GiantStag] = "GIANTSTAG1.png",
        [DonationType.MonitorLizard] = "MONITORLIZARD1.png",
        [DonationType.CritterMistCougar] = "CRITTERMISTCOUGAR1.png",
        [DonationType.Bear] = "BEAR.png",
        [DonationType.GiantSnake] = "GIANTSNAKE1.png",
        [DonationType.Marabou] = "MARABOU1.png",
        [DonationType.CritterMistCougar2] = "CRITTERMISTCOUGAR2.png",
        [DonationType.DireWolf] = "DIREWOLF.png",
        [DonationType.GiantStagMoose] = "GIANTSTAGMOOSE1.png",
        [DonationType.Alligator] = "ALLIGATOR1.png",
        [DonationType.DesertWolf] = "DESERTWOLF1.png",
        [DonationType.CritterMistCougar3] = "CRITTERMISTCOUGAR3.png",
        [DonationType.DireBoar2] = "DIREBOAR2.png",
        [DonationType.Rhino] = "RHINO1.png",
        [DonationType.Rhino2] = "RHINO2.png",
        [DonationType.DireBoar] = "DIREBOAR.png",
        [DonationType.CritterMistCougar4] = "CRITTERMISTCOUGAR4.png",
        [DonationType.DireBear2] = "DIREBEAR2.png",
        [DonationType.DireBear] = "DIREBEAR.png",
        [DonationType.CritterMistCougar5] = "CRITTERMISTCOUGAR5.png",
        [DonationType.Mammoth] = "MAMMOTH1.png",
        [DonationType.AncientMammoth] = "ANCIENTMAMMOTH1.png",
    };

    public static string GetResourcePath(object donationType)
    {
        return Folder + (donationType is DonationType type && Files.TryGetValue(type, out var file) ? file : DefaultFile);
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var path = GetResourcePath(value);
        if (Cache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        try
        {
            using var stream = AssetLoader.Open(new Uri(path));
            return Cache[path] = new Bitmap(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
