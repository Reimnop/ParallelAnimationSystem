using TmpParser;

namespace ParallelAnimationSystem.Rendering.TextProcessing;

public record TmpLine(
    float Ascender,
    float Descender,
    float Width,
    float Height,
    HorizontalAlignment? Alignment,
    ShapedGlyph[] Glyphs,
    Mark[] Marks);
