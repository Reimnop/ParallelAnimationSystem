using System.Numerics;
using Tmpx.Common;

namespace Tmpx.Generator;

public class GlyphPacker
{
    public List<QuadraticCurve> Curves { get; } = [];
    public List<int> CurveIndices { get; } = [];
    public List<BandEntry> Bands { get; } = [];
    public List<Glyph> Glyphs { get; } = [];

    public int AddGlyph(
        List<QuadraticCurve> curves,
        List<int> curveIndices,
        List<BandEntry> horizontalBandEntries,
        List<BandEntry> verticalBandEntries,
        float advanceWidth, Vector2 min, Vector2 max,
        GlyphColor color)
    {
        var curveBaseIndex = Curves.Count;
        Curves.AddRange(curves);
        
        var curveIndexBaseIndex = CurveIndices.Count;
        CurveIndices.AddRange(curveIndices.Select(i => i + curveBaseIndex));
        
        var horizontalBandBaseIndex = Bands.Count;
        Bands.AddRange(horizontalBandEntries.Select(x => x with { CurveIndexBaseIndex = x.CurveIndexBaseIndex + curveIndexBaseIndex }));
        
        var verticalBandBaseIndex = Bands.Count;
        Bands.AddRange(verticalBandEntries.Select(x => x with { CurveIndexBaseIndex = x.CurveIndexBaseIndex + curveIndexBaseIndex }));
        
        var horizontalBandCount = horizontalBandEntries.Count;
        var verticalBandCount = verticalBandEntries.Count;
        
        var horizontalBandScale = horizontalBandCount / (max.Y - min.Y);
        var verticalBandScale = verticalBandCount / (max.X - min.X);
        
        var horizontalBandOffset = -min.Y * (horizontalBandCount / (max.Y - min.Y));
        var verticalBandOffset = -min.X * (verticalBandCount / (max.X - min.X));

        var bands = new GlyphBands
        {
            HorizontalBandEntryBaseIndex = horizontalBandBaseIndex,
            HorizontalBandEntryCount = horizontalBandCount,
            HorizontalBandScale = horizontalBandScale,
            HorizontalBandOffset = horizontalBandOffset,
            
            VerticalBandEntryBaseIndex = verticalBandBaseIndex,
            VerticalBandEntryCount = verticalBandCount,
            VerticalBandScale = verticalBandScale,
            VerticalBandOffset = verticalBandOffset,
        };
        
        var metrics = new GlyphMetrics
        {
            AdvanceWidth = advanceWidth,
            Min = min,
            Max = max
        };
        
        var glyph = new Glyph
        {
            Bands = bands,
            Metrics = metrics,
            Color = color
        };
        var index = Glyphs.Count;
        Glyphs.Add(glyph);
        return index;
    }
}