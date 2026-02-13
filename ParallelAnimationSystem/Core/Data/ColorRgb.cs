using System.Numerics;
using System.Runtime.InteropServices;

namespace ParallelAnimationSystem.Core.Data;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ColorRgb(float r, float g, float b) : IEquatable<ColorRgb>
{
    public float R = r;
    public float G = g;
    public float B = b;

    public ColorRgb(byte r, byte g, byte b) : this(r / 255f, g / 255f, b / 255f)
    {
    }

    public ColorRgb(float value) : this(value, value, value)
    {
    }

    public static ColorRgb operator*(ColorRgb color, float scalar) 
        => new(color.R * scalar, color.G * scalar, color.B * scalar);
    
    public static ColorRgb Lerp(ColorRgb a, ColorRgb b, float t) 
        => new(
            float.Lerp(a.R, b.R, t),
            float.Lerp(a.G, b.G, t),
            float.Lerp(a.B, b.B, t));
    
    public static bool operator ==(ColorRgb left, ColorRgb right)
        => left.Equals(right);

    public static bool operator !=(ColorRgb left, ColorRgb right) 
        => !left.Equals(right);

    public bool Equals(ColorRgb other)
        => R == other.R && G == other.G && B == other.B;

    public override bool Equals(object? obj)
        => obj is ColorRgb other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(R, G, B);

    public Vector3 ToVector()
        => new(R, G, B);
}