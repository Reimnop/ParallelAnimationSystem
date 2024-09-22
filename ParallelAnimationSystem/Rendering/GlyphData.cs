using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Rendering;

[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct RenderGlyph(Vector2 min, Vector2 max, Vector2 minUV, Vector2 maxUV, Vector4 color, BoldItalic boldItalic, int fontIndex)
{
    public Vector2 Min = min; // 0 - 8
    public Vector2 Max = max; // 8 - 16
    public Vector2 MinUV = minUV; // 16 - 24
    public Vector2 MaxUV = maxUV; // 24 - 32
    public Vector4 Color = color; // 32 - 48
    public BoldItalic BoldItalic = boldItalic; // 48 - 52
    public int FontIndex = fontIndex; // 52 - 56
}