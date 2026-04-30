using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Tmpx.Common;

[StructLayout(LayoutKind.Sequential)]
public struct Color(byte r, byte g, byte b, byte a) : IEquatable<Color>
{
    public static readonly Color Transparent = new(0, 0, 0, 0);
    public static readonly Color Black = new(0, 0, 0, 255);
    public static readonly Color White = new(255, 255, 255, 255);
    
    public byte R = r;
    public byte G = g;
    public byte B = b;
    public byte A = a;
    
    public uint ToUInt32()
    {
        return (uint)R << 24 | (uint)G << 16 | (uint)B << 8 | A;
    }
    
    public static Color FromUInt32(uint color)
    {
        return new Color
        {
            R = (byte)(color >> 24),
            G = (byte)(color >> 16),
            B = (byte)(color >> 8),
            A = (byte)(color)
        };
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Color other &&
               R == other.R &&
               G == other.G &&
               B == other.B &&
               A == other.A;
    }

    public bool Equals(Color other)
    {
        return R == other.R && G == other.G && B == other.B && A == other.A;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }
}