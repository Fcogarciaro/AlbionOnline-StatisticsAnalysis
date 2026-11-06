using System.Text.RegularExpressions;
using System.Xml.Linq;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;

namespace StatisticsAnalysisTool.Avalonia.Tests.Theme;

public class ThemeColorsTests
{
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";

    internal static string WpfProjectDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "StatisticsAnalysisTool", "StatisticsAnalysisTool.csproj")))
        {
            dir = dir.Parent;
        }

        return dir == null ? throw new InvalidOperationException("src directory not found") : Path.Combine(dir.FullName, "StatisticsAnalysisTool");
    }

    private static bool Has(string key) =>
        global::Avalonia.Application.Current!.TryGetResource(key, null, out _);

    [AvaloniaTest]
    public void EveryKeyOfTheUpstreamColorsDictionaryExistsWithTheSameValue()
    {
        var upstream = XDocument.Load(Path.Combine(WpfProjectDirectory(), "Styles", "Colors.xaml"));
        var colors = upstream.Root!.Elements().Where(e => e.Name.LocalName == "Color").ToList();
        var brushes = upstream.Root.Elements().Where(e => e.Name.LocalName == "SolidColorBrush").ToList();
        colors.Count.Should().BeGreaterThan(200);

        foreach (var element in colors)
        {
            var key = (string) element.Attribute(X + "Key")!;
            Has(key).Should().BeTrue($"color {key} must be defined");
            global::Avalonia.Application.Current!.TryGetResource(key, null, out var value);
            value.Should().Be(global::Avalonia.Media.Color.Parse(element.Value.Trim()), key);
        }

        foreach (var element in brushes)
        {
            var key = (string) element.Attribute(X + "Key")!;
            Has(key).Should().BeTrue($"brush {key} must be defined");
        }
    }

    [AvaloniaTest]
    public void EveryLiteralColorOrBrushKeyUsedByTheLinkedCodeResolves()
    {
        var pattern = new Regex("\"((?:SolidColorBrush|Color)\\.[A-Za-z0-9.]+)\"");
        var keys = new SortedSet<string>();
        foreach (var file in Directory.EnumerateFiles(WpfProjectDirectory(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            foreach (Match m in pattern.Matches(File.ReadAllText(file)))
            {
                keys.Add(m.Groups[1].Value);
            }
        }

        keys.Should().Contain("Color.Accent.Blue.2").And.Contain("Color.Text.1").And.Contain("SolidColorBrush.Text.1");
        keys.Where(k => !Has(k)).Should().BeEmpty("all keys referenced from linked code must exist in the merged resources");
    }

    [AvaloniaTest]
    public void KeysBuiltFromEnumNamesResolveForTheNamesThatHaveBrushes()
    {
        foreach (var name in new[] { "Fame", "Silver", "ReSpec", "FactionStanding", "FactionPoints", "Might", "Favor" })
        {
            Has($"SolidColorBrush.Value.{name}").Should().BeTrue(name);
            Has($"SolidColorBrush.Value.{name}.Transparent").Should().BeTrue(name);
        }

        foreach (var name in new[] { "Wood", "Fiber", "Hide", "Ore", "Rock", "Fishing" })
        {
            Has($"SolidColorBrush.Resource.{name}").Should().BeTrue(name);
        }

        foreach (var name in new[] { "Default", "Caerleon", "Thetford", "Bridgewatch", "Martlock", "Lymhurst", "FortSterling", "Brecilien", "ArthursRest", "MerlynsRest", "MorganasRest", "BlackMarket", "SmugglersDen" })
        {
            Has($"Color.City.{name}").Should().BeTrue(name);
            Has($"SolidColorBrush.City.{name}.Transparent").Should().BeTrue(name);
        }
    }

    [AvaloniaTest]
    public void CompatApplicationResourcesResolveTheRealThemeKeys()
    {
        var color = (System.Windows.Media.Color) System.Windows.Application.Current.Resources["Color.Accent.Blue.2"]!;
        var brush = System.Windows.Application.Current.TryFindResource("SolidColorBrush.Text.1") as System.Windows.Media.SolidColorBrush;

        color.A.Should().Be(255);
        brush.Should().NotBeNull();
    }
}
