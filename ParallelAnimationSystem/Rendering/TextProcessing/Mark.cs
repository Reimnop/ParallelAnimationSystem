using TmpParser;

namespace ParallelAnimationSystem.Rendering.TextProcessing;

public record struct Mark(float MinX, float MinY, float MaxX, float MaxY, ColorAlpha Color)
{
    public float Width => MaxX - MinX;
    public float Height => MaxY - MinY;
}