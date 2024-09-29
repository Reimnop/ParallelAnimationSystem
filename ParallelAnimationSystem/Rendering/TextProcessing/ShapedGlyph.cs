namespace ParallelAnimationSystem.Rendering.TextProcessing;

public record struct ShapedGlyph(float Position, float YOffset, int FontIndex, int GlyphId, float Size, Style Style);