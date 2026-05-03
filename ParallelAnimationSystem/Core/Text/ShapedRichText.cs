using System.Numerics;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Text;

/// <summary>
/// One placed glyph (or mark rectangle when <see cref="ShapeEntryIndex"/> == -1) produced by the shaper.
/// <see cref="ShapeEntryIndex"/> is already in global buffer space — FontService remaps per-font-local
/// indices to global ones during shaping so the renderer never needs to know about fonts at all.
/// </summary>
public struct ShapedTextGlyph(Matrix3x2 transform, ColorRgba color, int shapeEntryIndex)
{
    public Matrix3x2 Transform = transform;
    public ColorRgba Color = color;

    /// <summary>Global index into the renderer's shape-entry buffer. -1 = mark rectangle.</summary>
    public int ShapeEntryIndex = shapeEntryIndex;
}

public class ShapedRichText
{
    public List<ShapedTextGlyph> Glyphs { get; set; } = [];
}
