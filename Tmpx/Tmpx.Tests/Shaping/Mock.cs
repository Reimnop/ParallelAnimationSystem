using System.Diagnostics.CodeAnalysis;
using Tmpx.Common;
using Tmpx.Shaping;

namespace Tmpx.Tests.Shaping;

public class MockFont(string familyName, FontStyle style, FontMetrics metrics, Dictionary<char, IGlyph> glyphs) : IFont
{
    public string FamilyName => familyName;
    public string StyleName => style.ToString();
    public FontStyle Style => style;
    public IFontMetrics Metrics => metrics;
    
    public bool TryGetGlyph(char c, [MaybeNullWhen(false)] out IGlyph glyph)
        => glyphs.TryGetValue(c, out glyph);

    public bool TryGetSprite(string name, [MaybeNullWhen(false)] out ISprite sprite)
    {
        throw new NotImplementedException();
    }

    public float GetKerning(char left, char right)
    {
        throw new NotImplementedException();
    }
}

public class MockFontResolver : IFontResolver
{
    private readonly Dictionary<(string, FontStyle), IFont> fonts = new();

    public void Register(IFont font, FontStyle style)
        => fonts[(font.FamilyName, style)] = font;

    public bool TryResolve(string family, FontStyle style, out IFont font)
        => fonts.TryGetValue((family, style), out font!);
}

public static class MockFontFactory
{
    public static readonly FontMetrics DefaultMetrics = new()
    {
        Ascender =  0.8f,
        Descender = -0.2f,
        LineHeight =  1.0f,
    };

    // creates a font where every ASCII printable character has the given advance width
    public static MockFont Create(string family, FontStyle style = FontStyle.Regular, float advanceWidth = 1f, FontMetrics? metrics = null)
    {
        var glyphs = Enumerable.Range(32, 95)
            .ToDictionary(i => (char)i, IGlyph (i) => new Glyph { AdvanceWidth = advanceWidth, ShapeEntryIndex = i });

        return new MockFont(family, style, metrics ?? DefaultMetrics, glyphs);
    }
}