using System.Text.RegularExpressions;
using DSaladin.FontAwesome.WPF;
using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Avalonia.Tests.Theme;
using StatisticsAnalysisTool.Linux.Icons;

namespace StatisticsAnalysisTool.Avalonia.Tests.Icons;

public class FontAwesomeGlyphsTests
{
    [TestCase(FontAwesomeIcon.SolidCircleInfo, FontAwesomeStyle.Solid, 0xF05A)]
    [TestCase(FontAwesomeIcon.RegularClock, FontAwesomeStyle.Regular, 0xF017)]
    [TestCase(FontAwesomeIcon.BrandsPaypal, FontAwesomeStyle.Brands, 0xF1ED)]
    [TestCase(FontAwesomeIcon.SolidMinus, FontAwesomeStyle.Solid, 0xF068)]
    [TestCase(FontAwesomeIcon.Solid1, FontAwesomeStyle.Solid, 0x31)]
    [TestCase(FontAwesomeIcon.Brands500Px, FontAwesomeStyle.Brands, 0xF26E)]
    public void Lookup_ReturnsTheStyleAndCodepointOfTheIcon(FontAwesomeIcon icon, FontAwesomeStyle style, int codePoint)
    {
        FontAwesomeGlyphs.TryGet(icon, out var glyph).Should().BeTrue();

        glyph.Style.Should().Be(style);
        glyph.CodePoint.Should().Be(codePoint);
    }

    [Test]
    public void None_HasNoGlyph()
    {
        FontAwesomeGlyphs.TryGet(FontAwesomeIcon.None, out _).Should().BeFalse();
        FontAwesomeGlyphs.TryGet((FontAwesomeIcon) 999999, out _).Should().BeFalse();
    }

    [Test]
    public void EveryEnumMemberHasAGlyphOfItsOwnStyle()
    {
        var members = Enum.GetNames<FontAwesomeIcon>().Where(n => n is not "None" and not "Solid0").ToList();
        members.Count.Should().BeGreaterThan(2000);

        foreach (var name in members)
        {
            var icon = Enum.Parse<FontAwesomeIcon>(name);
            FontAwesomeGlyphs.TryGet(icon, out var glyph).Should().BeTrue(name);
            name.Should().StartWith(glyph.Style.ToString(), "the member prefix names the font style");
        }
    }

    [Test]
    public void EveryIconUsedByTheUpstreamXamlResolves()
    {
        var pattern = new Regex(@"<fa:FontAwesomeImage[^>]*?Icon=""(\w+)""", RegexOptions.Singleline);
        var names = new SortedSet<string>();
        foreach (var file in Directory.EnumerateFiles(ThemeColorsTests.WpfProjectDirectory(), "*.xaml", SearchOption.AllDirectories))
        {
            foreach (Match match in pattern.Matches(File.ReadAllText(file)))
            {
                names.Add(match.Groups[1].Value);
            }
        }

        names.Should().Contain("SolidCircleInfo").And.Contain("RegularClock");
        foreach (var name in names)
        {
            Enum.TryParse<FontAwesomeIcon>(name, out var icon).Should().BeTrue(name);
            FontAwesomeGlyphs.TryGet(icon, out _).Should().BeTrue(name);
        }
    }

    [Test]
    public void FontFamilyNames_MatchTheEmbeddedFonts()
    {
        FontAwesomeGlyphs.FontFamilyName(FontAwesomeStyle.Solid).Should().Be("Font Awesome 6 Free");
        FontAwesomeGlyphs.FontFamilyName(FontAwesomeStyle.Regular).Should().Be("Font Awesome 6 Free");
        FontAwesomeGlyphs.FontFamilyName(FontAwesomeStyle.Brands).Should().Be("Font Awesome 6 Brands");
    }
}
