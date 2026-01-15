using System.Runtime.InteropServices;
using System.Numerics;

namespace ParallelAnimationSystem.Rendering.OpenGL;

// std430 layout
[StructLayout(LayoutKind.Explicit, Size = 80)]
public struct MultiDrawItem
{
    [FieldOffset(0)] public required Matrix3x2 Mvp;
    [FieldOffset(32)] public required Vector4 Color1;
    [FieldOffset(48)] public required Vector4 Color2;
    [FieldOffset(64)] public required float Z;
    [FieldOffset(68)] public required int RenderMode;
    [FieldOffset(72)] public required int RenderType;
    [FieldOffset(76)] public required int GlyphOffset;
}