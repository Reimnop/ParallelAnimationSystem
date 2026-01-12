using System.Numerics;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Core.Data;

public struct ColorRgba(float r, float g, float b, float a) : IEquatable<ColorRgba>
{
    public float R => r;
    public float G => g;
    public float B => b;
    public float A => a;
    
    public static ColorRgba operator*(ColorRgba color, float scalar) 
        => new(color.R * scalar, color.G * scalar, color.B * scalar, color.A * scalar);
    
    public static ColorRgba Lerp(ColorRgba a, ColorRgba b, float t) 
        => new(
            MathUtil.Lerp(a.R, b.R, t),
            MathUtil.Lerp(a.G, b.G, t),
            MathUtil.Lerp(a.B, b.B, t),
            MathUtil.Lerp(a.A, b.A, t));
    
    public static bool operator ==(ColorRgba left, ColorRgba right)
        => left.Equals(right);

    public static bool operator !=(ColorRgba left, ColorRgba right) 
        => !left.Equals(right);

    public bool Equals(ColorRgba other)
        => R == other.R && G == other.G && B == other.B && A == other.A;

    public override bool Equals(object? obj)
        => obj is ColorRgba other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(R, G, B, A);

    public Vector4 ToVector()
        => new(R, G, B, A);
}