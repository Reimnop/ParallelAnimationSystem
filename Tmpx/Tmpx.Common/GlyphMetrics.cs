using System.Numerics;
using System.Runtime.InteropServices;

namespace Tmpx.Common;

[StructLayout(LayoutKind.Sequential)]
public struct GlyphMetrics
{
    public float AdvanceWidth;
    public Vector2 Min;
    public Vector2 Max;
}