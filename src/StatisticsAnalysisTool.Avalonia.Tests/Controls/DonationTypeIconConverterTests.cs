using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Avalonia.Tests.Views;
using StatisticsAnalysisTool.Common.Converters;
using StatisticsAnalysisTool.Enumerations;
using System.Text.RegularExpressions;

namespace StatisticsAnalysisTool.Avalonia.Tests.Controls;

public class DonationTypeIconConverterTests
{
    [Test]
    public void DonationTypeIconConverter_MapsEveryDonationTypeLikeTheUpstreamTriggers()
    {
        var upstream = File.ReadAllText(UpstreamPaths.File("Styles/DonationStyles.xaml"));
        var pairs = Regex.Matches(upstream, @"DonationType\.(\w+)\}"">\s*<Setter Property=""Source"" Value=""\.\./Resources/DonationIcon/(\w+\.png)""")
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);

        pairs.Should().HaveCount(Enum.GetValues<DonationType>().Length);
        foreach (var (name, file) in pairs)
        {
            DonationTypeIconConverter.GetResourcePath(Enum.Parse<DonationType>(name)).Should().Be($"avares://StatisticsAnalysisTool/Resources/DonationIcon/{file}");
        }
    }

    [Test]
    public void DonationTypeIconConverter_FallsBackToTheToadLikeTheUpstreamDefault()
    {
        DonationTypeIconConverter.GetResourcePath(null).Should().EndWith("DonationIcon/TOAD1.png");
        DonationTypeIconConverter.GetResourcePath("nonsense").Should().EndWith("DonationIcon/TOAD1.png");
    }
}
