using System.Runtime.InteropServices;

namespace Tmpx.Common;

[StructLayout(LayoutKind.Sequential)]
public struct GlyphBands
{
    public int HorizontalBandEntryBaseIndex;
    public int HorizontalBandEntryCount;
    public float HorizontalBandScale;
    public float HorizontalBandOffset;
    
    public int VerticalBandEntryBaseIndex;
    public int VerticalBandEntryCount;
    public float VerticalBandScale;
    public float VerticalBandOffset;
}