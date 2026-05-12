using System.Numerics;
using System.Runtime.InteropServices;

namespace Tmpx.Common;

[StructLayout(LayoutKind.Sequential)]
public struct ShapeEntry
{
    public int HorizontalBandEntryBaseIndex;
    public int HorizontalBandEntryCount;
    public float HorizontalBandScale;
    public float HorizontalBandOffset;
    
    public int VerticalBandEntryBaseIndex;
    public int VerticalBandEntryCount;
    public float VerticalBandScale;
    public float VerticalBandOffset;
    
    public Vector2 Min;
    public Vector2 Max;
    
    public Color Color;
}