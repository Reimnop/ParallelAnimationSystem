using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Text;

public struct ShapedTextGlyph(
    float minX, float minY, float maxX, float maxY,
    float minU, float minV, float maxU, float maxV,
    ColorRgba color, BoldItalic boldItalic, int fontId)
{
    public Vector2 Min = new(minX, minY);
    public Vector2 Max = new(maxX, maxY);
    public Vector2 MinUV = new(minU, minV);
    public Vector2 MaxUV = new(maxU, maxV);
    public ColorRgba Color = color;
    public BoldItalic BoldItalic = boldItalic;
    public int FontId = fontId;
}