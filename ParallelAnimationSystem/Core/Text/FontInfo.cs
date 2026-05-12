using Tmpx.Common;
using Tmpx.Shaping;

namespace ParallelAnimationSystem.Core.Text;

public class FontInfo(string name, FontStyle style, Font font, int globalShapeEntryBase)
{
    public string Name => name;
    public FontStyle Style => style;
    public Font Font => font;
    
    /// <summary>
    /// Offset into the global ShapeEntries buffer where this font's entries begin.
    /// Shaper-emitted local indices must be offset by this value before being stored in
    /// <see cref="ShapedTextGlyph.ShapeEntryIndex"/>.
    /// </summary>
    public int GlobalShapeEntryBase => globalShapeEntryBase;
}
