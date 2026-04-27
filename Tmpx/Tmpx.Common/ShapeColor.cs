using System.Runtime.InteropServices;

namespace Tmpx.Common;

[StructLayout(LayoutKind.Sequential)]
public struct ShapeColor
{
    public static readonly ShapeColor Black = new() { R = 0, G = 0, B = 0, A = 255 };
    public static readonly ShapeColor White = new() { R = 255, G = 255, B = 255, A = 255 };
    
    public byte R;
    public byte G;
    public byte B;
    public byte A;
    
    public uint ToUInt32()
    {
        return (uint)(R << 24 | G << 16 | B << 8 | A);
    }
    
    public static ShapeColor FromUInt32(uint color)
    {
        return new ShapeColor
        {
            R = (byte)(color >> 24),
            G = (byte)(color >> 16),
            B = (byte)(color >> 8),
            A = (byte)(color)
        };
    }
}