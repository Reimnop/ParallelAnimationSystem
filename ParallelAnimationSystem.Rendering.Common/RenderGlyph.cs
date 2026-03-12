using System.Numerics;
using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Rendering.Common;

[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct RenderGlyph
{
    [FieldOffset(0)] public Vector2 Min; // 0 - 8
    [FieldOffset(8)] public Vector2 Max; // 8 - 16
    [FieldOffset(16)] public Vector2 MinUV; // 16 - 24
    [FieldOffset(24)] public Vector2 MaxUV; // 24 - 32
    [FieldOffset(32)] public ColorRgba Color; // 32 - 48
    [FieldOffset(48)] public float Rotation; // 48 - 52
    [FieldOffset(52)] public BoldItalic BoldItalic; // 52 - 56
    [FieldOffset(56)] public int AtlasIndex; // 56 - 60
}