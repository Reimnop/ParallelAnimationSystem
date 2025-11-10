using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Data;

public struct BeatmapObjectColor(ColorRgba color1, ColorRgba color2, float opacity) : IEquatable<BeatmapObjectColor>
{
    public ColorRgba Color1 => color1;
    public ColorRgba Color2 => color2;
    public float Opacity => opacity;
    
    public static BeatmapObjectColor Lerp(BeatmapObjectColor a, BeatmapObjectColor b, float t)
        => new(
            ColorRgba.Lerp(a.Color1, b.Color1, t),
            ColorRgba.Lerp(a.Color2, b.Color2, t),
            MathUtil.Lerp(a.Opacity, b.Opacity, t));
    
    public static bool operator ==(BeatmapObjectColor left, BeatmapObjectColor right)
        => left.Equals(right);

    public static bool operator !=(BeatmapObjectColor left, BeatmapObjectColor right) 
        => !left.Equals(right);

    public bool Equals(BeatmapObjectColor other)
        => Color1 == other.Color1 && Color2 == other.Color2 && Opacity == other.Opacity;

    public override bool Equals(object? obj)
        => obj is BeatmapObjectColor other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Color1, Color2, Opacity);
}