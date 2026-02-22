using System.Runtime.InteropServices;
using System.Numerics;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

[StructLayout(LayoutKind.Explicit, Size = 56)]
public struct RenderGlyph
{
    [FieldOffset(0)] public Vector2 Min; // 0 - 8
    [FieldOffset(8)] public Vector2 Max; // 8 - 16
    [FieldOffset(16)] public Vector2 MinUV; // 16 - 24
    [FieldOffset(24)] public Vector2 MaxUV; // 24 - 32
    [FieldOffset(32)] public ColorRgba Color; // 32 - 48
    [FieldOffset(48)] public BoldItalic BoldItalic; // 48 - 52
    [FieldOffset(52)] public int FontIndex; // 52 - 56
}