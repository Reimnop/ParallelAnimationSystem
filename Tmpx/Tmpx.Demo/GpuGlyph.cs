using System.Numerics;
using System.Runtime.InteropServices;
using Tmpx.Common;

namespace Tmpx.Demo;

[StructLayout(LayoutKind.Sequential, Size = 56)]
public struct GpuGlyph
{
    // -- 0 - 32 (32 bytes) --
    public int HorizontalBandEntryBaseIndex;
    public int HorizontalBandEntryCount;
    public float HorizontalBandScale;
    public float HorizontalBandOffset;
    public int VerticalBandEntryBaseIndex;
    public int VerticalBandEntryCount;
    public float VerticalBandScale;
    public float VerticalBandOffset;
    // -----------------------
    
    public Vector2 Min; // 32 - 40 (8-byte aligned)
    public Vector2 Max; // 40 - 48
    
    public GlyphColor Color; // 48 - 52
}