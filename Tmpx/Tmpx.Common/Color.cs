using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    
    public static Color ParseHex(string hex)
    {
        return new Color(
            byte.Parse(hex[..2], NumberStyles.HexNumber),
            byte.Parse(hex[2..4], NumberStyles.HexNumber),
            byte.Parse(hex[4..6], NumberStyles.HexNumber),
            255
        );
    }
    
    public static bool TryParseHex(string hex, out Color color)
    {
        color = default;
        
        if (string.IsNullOrWhiteSpace(hex))
            return false;
        
        if (hex.Length != 6)
            return false;
        
        if (byte.TryParse(hex[..2], NumberStyles.HexNumber, null, out var r) &&
            byte.TryParse(hex[2..4], NumberStyles.HexNumber, null, out var g) &&
            byte.TryParse(hex[4..6], NumberStyles.HexNumber, null, out var b))
        {
            color = new Color(r, g, b, 255);
            return true;
        }
        
        return false;
    }
}