using System.Diagnostics.CodeAnalysis;

namespace Tmpx.Common;

public class Font : IFont
{
    public string FamilyName { get; set; } = string.Empty;
    public string StyleName { get; set; } = string.Empty;
    
    public FontMetrics Metrics { get; set; } = new();
    IFontMetrics IFont.Metrics => Metrics;
    
    public Dictionary<string, Sprite> SpriteMap { get; set; } = new();
    public Dictionary<char, Glyph> GlyphMap { get; set; } = new();
    public Dictionary<(char, char), float> Kerning { get; set; } = new();
    
    public QuadraticCurve[] Curves { get; set; } = [];
    public int[] CurveIndices { get; set; } = [];
    public BandEntry[] BandEntries { get; set; } = [];
    public ShapeEntry[] ShapeEntries { get; set; } = [];

    public bool TryGetGlyph(char c, [MaybeNullWhen(false)] out IGlyph glyph)
    {
        glyph = null;
        
        if (GlyphMap.TryGetValue(c, out var g))
        {
            glyph = g;
            return true;
        }
        
        return false;
    }

    public bool TryGetSprite(string name, [MaybeNullWhen(false)] out ISprite sprite)
    {
        sprite = null;
        
        if (SpriteMap.TryGetValue(name, out var s))
        {
            sprite = s;
            return true;
        }
        
        return false;
    }

    public float GetKerning(char left, char right)
        => Kerning.GetValueOrDefault((left, right), 0f);
}