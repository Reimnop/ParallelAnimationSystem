using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

[StructLayout(LayoutKind.Explicit, Size = 96)]
public struct RenderGlyph(Vector2 min, Vector2 max, Vector2 minUV, Vector2 maxUV, Vector4 color, BoldItalic boldItalic, int fontIndex)
{
    [FieldOffset(0)] public Vector2 Min = min; // 0 - 16
    [FieldOffset(16)] public Vector2 Max = max; // 16 - 32
    [FieldOffset(32)] public Vector2 MinUV = minUV; // 32 - 48
    [FieldOffset(48)] public Vector2 MaxUV = maxUV; // 48 - 64
    [FieldOffset(64)] public Vector4 Color = color; // 64 - 80
    [FieldOffset(80)] public BoldItalic BoldItalic = boldItalic; // 80 - 84
    [FieldOffset(84)] public int FontIndex = fontIndex; // 84 - 88
}