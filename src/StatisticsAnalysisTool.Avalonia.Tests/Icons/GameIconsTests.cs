using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using FluentAssertions;
using MahApps.Metro.IconPacks;
using NUnit.Framework;
using StatisticsAnalysisTool.Avalonia.Tests.Theme;
using StatisticsAnalysisTool.Linux.Icons;

namespace StatisticsAnalysisTool.Avalonia.Tests.Icons;

public class GameIconsTests
{
    private const string Namespaces = "xmlns:iconPacks=\"clr-namespace:MahApps.Metro.IconPacks\"";

    [TestCase(PackIconGameIconsKind.Info, "M256 21C126.426 21")]
    [TestCase(PackIconGameIconsKind.Hourglass, "M92.656 19.188v41.5h331.72")]
    [TestCase(PackIconGameIconsKind.ShieldDisabled, "M486 68.125 68.125 486")]
    [TestCase(PackIconGameIconsKind.Broadsword, "m491.844 22.533-83.42")]
    public void PathData_IsTheMahAppsDataOfTheKind(PackIconGameIconsKind kind, string prefix)
    {
        GameIconsData.TryGetPathData(kind, out var data).Should().BeTrue();

        data.Should().StartWith(prefix);
    }

    [Test]
    public void None_AndUnknownKinds_HaveNoPathData()
    {
        GameIconsData.TryGetPathData(PackIconGameIconsKind.None, out _).Should().BeFalse();
        GameIconsData.TryGetPathData((PackIconGameIconsKind) 12345, out _).Should().BeFalse();
    }

    [Test]
    public void EveryKindUsedByTheUpstreamXamlHasAnIcon()
    {
        var kinds = new SortedSet<string>();
        foreach (var file in Directory.EnumerateFiles(ThemeColorsTests.WpfProjectDirectory(), "*.xaml", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(text, @"PackIconGameIcons[^>]*?\sKind=""(\w+)""", RegexOptions.Singleline))
            {
                kinds.Add(m.Groups[1].Value);
            }

            foreach (Match m in Regex.Matches(text, @"Property=""Kind""\s+Value=""(\w+)"""))
            {
                kinds.Add(m.Groups[1].Value);
            }
        }

        kinds.Should().HaveCountGreaterThanOrEqualTo(12).And.Contain(["Broadsword", "CrystalWand", "ShieldDisabled", "Info"]);
        foreach (var name in kinds)
        {
            Enum.TryParse<PackIconGameIconsKind>(name, out var kind).Should().BeTrue(name);
            GameIconsData.TryGetPathData(kind, out var data).Should().BeTrue(name);
            data.Should().NotBeNullOrWhiteSpace(name);
        }
    }

    [AvaloniaTest]
    public void PathData_ParsesIntoGeometriesInsideTheIconViewBox()
    {
        foreach (var kind in Enum.GetValues<PackIconGameIconsKind>().Where(k => k != PackIconGameIconsKind.None))
        {
            var geometry = GameIconsData.CreateGeometry(kind);

            geometry.Should().NotBeNull(kind.ToString());
            geometry!.Bounds.Width.Should().BeGreaterThan(50, kind.ToString());
            geometry.Bounds.Left.Should().BeGreaterThanOrEqualTo(-1);
            geometry.Bounds.Right.Should().BeLessThanOrEqualTo(513);
        }
    }

    [AvaloniaTest]
    public void Control_DrawsTheIconInItsForeground()
    {
        var window = SharedStylesTests.Load("""<iconPacks:PackIconGameIcons Kind="Info" Width="48" Height="48" Foreground="#FF00FF00" HorizontalAlignment="Left"/>""", Namespaces);

        FontAwesomeImageTests.CountPixelsIn(window, p => p.G > 200 && p.R < 60 && p.B < 60 && p.A > 200).Should().BeGreaterThan(300);
        window.Close();
    }

    [AvaloniaTest]
    public void Control_FollowsKindChangesAndDefaultsToTheMahAppsSize()
    {
        var window = SharedStylesTests.Load("""<iconPacks:PackIconGameIcons x:Name="icon" Foreground="#FF00FF00" HorizontalAlignment="Left"/>""", Namespaces);
        var icon = window.FindControl<PackIconGameIcons>("icon")!;

        icon.Kind.Should().Be(PackIconGameIconsKind.None);
        FontAwesomeImageTests.CountPixelsIn(window, p => p.G > 200 && p.R < 60).Should().Be(0, "None draws nothing");
        icon.Kind = PackIconGameIconsKind.Healing;
        FontAwesomeImageTests.CountPixelsIn(window, p => p.G > 200 && p.R < 60).Should().BeGreaterThan(100);
        icon.Bounds.Width.Should().Be(26);
        icon.Bounds.Height.Should().Be(26);
        window.Close();
    }

    [AvaloniaTest]
    public void Kind_CanBeSetByAStyleSetter()
    {
        var window = SharedStylesTests.Load("""
            <iconPacks:PackIconGameIcons x:Name="icon" Width="30" Height="30">
              <iconPacks:PackIconGameIcons.Styles>
                <Style Selector="iconPacks|PackIconGameIcons"><Setter Property="Kind" Value="Broadsword"/></Style>
              </iconPacks:PackIconGameIcons.Styles>
            </iconPacks:PackIconGameIcons>
            """, Namespaces);

        window.FindControl<PackIconGameIcons>("icon")!.Kind.Should().Be(PackIconGameIconsKind.Broadsword);
        window.Close();
    }
}
