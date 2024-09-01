using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Rendering;

// std430 layout
[StructLayout(LayoutKind.Explicit, Size = 96)]
public struct MultiDrawItem
{
    [FieldOffset(0)] public Matrix3 ModelViewProjection;
    [FieldOffset(48)] public Vector4 Color1;
    [FieldOffset(64)] public Vector4 Color2;
    [FieldOffset(80)] public float Z;
    [FieldOffset(84)] public int RenderMode;
}