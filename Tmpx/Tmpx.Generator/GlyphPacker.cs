using System.Numerics;
using Tmpx.Common;

namespace Tmpx.Generator;

public class GlyphPacker
{
    public List<QuadraticCurve> Curves { get; } = [];
    public List<int> CurveIndices { get; } = [];
    public List<BandEntry> BandEntries { get; } = [];
    public List<ShapeEntry> ShapeEntries { get; } = [];

    public Glyph AddGlyph(
        List<QuadraticCurve> curves,
        List<int> curveIndices,
        List<BandEntry> horizontalBandEntries,
        List<BandEntry> verticalBandEntries,
        float advanceWidth, Vector2 min, Vector2 max,
        ShapeColor color)
    {
        var curveBaseIndex = Curves.Count;
        Curves.AddRange(curves);
        
        var curveIndexBaseIndex = CurveIndices.Count;
        CurveIndices.AddRange(curveIndices.Select(i => i + curveBaseIndex));
        
        var horizontalBandBaseIndex = BandEntries.Count;
        BandEntries.AddRange(horizontalBandEntries.Select(x => x with { CurveIndexBaseIndex = x.CurveIndexBaseIndex + curveIndexBaseIndex }));
        
        var verticalBandBaseIndex = BandEntries.Count;
        BandEntries.AddRange(verticalBandEntries.Select(x => x with { CurveIndexBaseIndex = x.CurveIndexBaseIndex + curveIndexBaseIndex }));
        
        var horizontalBandCount = horizontalBandEntries.Count;
        var verticalBandCount = verticalBandEntries.Count;
        
        var horizontalBandScale = horizontalBandCount / (max.Y - min.Y);
        var verticalBandScale = verticalBandCount / (max.X - min.X);
        
        var horizontalBandOffset = -min.Y * (horizontalBandCount / (max.Y - min.Y));
        var verticalBandOffset = -min.X * (verticalBandCount / (max.X - min.X));

        var shapeEntry = new ShapeEntry
        {
            HorizontalBandEntryBaseIndex = horizontalBandBaseIndex,
            HorizontalBandEntryCount = horizontalBandCount,
            HorizontalBandScale = horizontalBandScale,
            HorizontalBandOffset = horizontalBandOffset,
            
            VerticalBandEntryBaseIndex = verticalBandBaseIndex,
            VerticalBandEntryCount = verticalBandCount,
            VerticalBandScale = verticalBandScale,
            VerticalBandOffset = verticalBandOffset,
            
            Min = min,
            Max = max,
            
            Color = color
        };
        var shapeEntryIndex = ShapeEntries.Count;
        ShapeEntries.Add(shapeEntry);
        
        var glyph = new Glyph
        {
            AdvanceWidth = advanceWidth,
            ShapeEntryIndex = shapeEntryIndex
        };
        return glyph;
    }
}