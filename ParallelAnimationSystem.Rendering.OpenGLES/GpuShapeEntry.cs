using System.Numerics;
using System.Runtime.InteropServices;
using Tmpx.Common;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct GpuShapeEntry
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
    
    public Vector2 Min; // 32 - 40
    public Vector2 Max; // 40 - 48
    
    public Color Color; // 48 - 52
}