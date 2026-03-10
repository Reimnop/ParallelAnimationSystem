using System.Runtime.InteropServices;
using System.Numerics;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Rendering.OpenGL;

// std430 layout
[StructLayout(LayoutKind.Explicit, Size = 96)]
public struct MultiDrawItem
{
    [FieldOffset(0)] public Matrix3x2 Mvp;
    [FieldOffset(32)] public ColorRgba Color1;
    [FieldOffset(48)] public ColorRgba Color2;
    [FieldOffset(64)] public float Z;
    [FieldOffset(68)] public int RenderMode;
    [FieldOffset(72)] public int RenderType;
    [FieldOffset(76)] public int GlyphOffset;
    [FieldOffset(80)] public float GradientRotation;
    [FieldOffset(84)] public float GradientScale;
}