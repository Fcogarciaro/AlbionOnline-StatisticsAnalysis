// Maps DSaladin.FontAwesome.WPF.FontAwesomeIcon members to Font Awesome Free 6.7.2 glyphs (style + codepoint, see
// FontAwesomeGlyphs.g.cs) and builds their outline geometry from the fonts embedded in Assets/Fonts.
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using DSaladin.FontAwesome.WPF;

namespace StatisticsAnalysisTool.Linux.Icons;

public enum FontAwesomeStyle
{
    Solid,
    Regular,
    Brands
}

public readonly record struct FontAwesomeGlyph(FontAwesomeStyle Style, int CodePoint);

internal static partial class FontAwesomeGlyphs
{
    // A font folder: Avalonia registers every font of Assets/Fonts and picks the face by family name and weight.
    private const string FontsUri = "avares://StatisticsAnalysisTool/Assets/Fonts";
    private const double OutlineEmSize = 100;

    private static readonly Dictionary<FontAwesomeIcon, FontAwesomeGlyph> Table = BuildTable();
    private static readonly Dictionary<FontAwesomeIcon, Geometry> Geometries = new();

    public static bool TryGet(FontAwesomeIcon icon, out FontAwesomeGlyph glyph) => Table.TryGetValue(icon, out glyph);

    public static string FontFamilyName(FontAwesomeStyle style) => style == FontAwesomeStyle.Brands ? "Font Awesome 6 Brands" : "Font Awesome 6 Free";

    public static Typeface GetTypeface(FontAwesomeStyle style)
    {
        // Solid is the 900 weight face of "Font Awesome 6 Free", Regular the 400 one.
        var weight = style == FontAwesomeStyle.Solid ? FontWeight.Black : FontWeight.Normal;
        return new Typeface(new FontFamily($"{FontsUri}#{FontFamilyName(style)}"), FontStyle.Normal, weight);
    }

    public static bool FontHasGlyph(FontAwesomeGlyph glyph)
    {
        return FontManager.Current.TryGetGlyphTypeface(GetTypeface(glyph.Style), out var typeface)
               && typeface.TryGetGlyph((uint) glyph.CodePoint, out var glyphIndex)
               && glyphIndex != 0;
    }

    /// <summary>The glyph outline (cached per icon), or null for icons without a glyph.</summary>
    public static Geometry CreateGeometry(FontAwesomeIcon icon)
    {
        if (!TryGet(icon, out var glyph))
        {
            return null;
        }

        lock (Geometries)
        {
            if (Geometries.TryGetValue(icon, out var cached))
            {
                return cached;
            }

            var text = new FormattedText(char.ConvertFromUtf32(glyph.CodePoint), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                GetTypeface(glyph.Style), OutlineEmSize, Brushes.Black);
            var geometry = text.BuildGeometry(new global::Avalonia.Point(0, 0));
            if (geometry != null)
            {
                Geometries[icon] = geometry;
            }

            return geometry;
        }
    }
}
