using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.Handle;

namespace ParallelAnimationSystem.Core.Text;

public class ShapedTextGlyph(
    Vector2 min, Vector2 max,
    Vector2 minUV, Vector2 maxUV,
    ColorRgba color, BoldItalic boldItalic,
    FontHandle? font)
{
    public Vector2 Min { get; set; } = min;
    public Vector2 Max { get; set; } = max;
    public Vector2 MinUV { get; set; } = minUV;
    public Vector2 MaxUV { get; set; } = maxUV;
    public ColorRgba Color { get; set; } = color;
    public BoldItalic BoldItalic { get; set; } = boldItalic;
    public FontHandle? Font { get; set; } = font;
    
    public ShapedTextGlyph(
        float minX, float minY, float maxX, float maxY,
        float minU, float minV, float maxU, float maxV,
        ColorRgba color, BoldItalic boldItalic, FontHandle? font)
        : this(new Vector2(minX, minY), new Vector2(maxX, maxY), 
            new Vector2(minU, minV), new Vector2(maxU, maxV), 
            color, boldItalic,
            font)
    {
    }
}

public class ShapedRichText
{
    public List<ShapedTextGlyph> Glyphs { get; set; } = [];
}