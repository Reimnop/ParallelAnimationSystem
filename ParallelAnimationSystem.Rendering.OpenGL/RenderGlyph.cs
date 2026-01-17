using System.Runtime.InteropServices;
using System.Numerics;

namespace ParallelAnimationSystem.Rendering.OpenGL;

[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct RenderGlyph(Vector2 min, Vector2 max, Vector2 minUV, Vector2 maxUV, Vector4 color, BoldItalic boldItalic, int fontIndex)
{
    [FieldOffset(0)] public Vector2 Min = min; // 0 - 8
    [FieldOffset(8)] public Vector2 Max = max; // 8 - 16
    [FieldOffset(16)] public Vector2 MinUV = minUV; // 16 - 24
    [FieldOffset(24)] public Vector2 MaxUV = maxUV; // 24 - 32
    [FieldOffset(32)] public Vector4 Color = color; // 32 - 48
    [FieldOffset(48)] public BoldItalic BoldItalic = boldItalic; // 48 - 52
    [FieldOffset(52)] public int FontIndex = fontIndex; // 52 - 56
}