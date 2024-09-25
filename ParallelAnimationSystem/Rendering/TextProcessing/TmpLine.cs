using TmpParser;

namespace ParallelAnimationSystem.Rendering.TextProcessing;

public record TmpLine(
    float Ascender,
    float Descender,
    float Width,
    float Height,
    float AdvanceY,
    HorizontalAlignment? Alignment,
    ShapedGlyph[] Glyphs,
    Mark[] Marks);
