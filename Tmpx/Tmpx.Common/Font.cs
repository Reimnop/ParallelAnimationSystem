namespace Tmpx.Common;

public class Font
{
    public string FamilyName { get; set; } = string.Empty;
    public string StyleName { get; set; } = string.Empty;
    public FontMetrics Metrics { get; set; } = new();
    public Dictionary<string, Sprite> SpriteMap { get; set; } = new();
    public Dictionary<char, Glyph> GlyphMap { get; set; } = new();
    public Dictionary<(char, char), float> Kerning { get; set; } = new();
    
    public QuadraticCurve[] Curves { get; set; } = [];
    public int[] CurveIndices { get; set; } = [];
    public BandEntry[] BandEntries { get; set; } = [];
    public ShapeEntry[] ShapeEntries { get; set; } = [];
}