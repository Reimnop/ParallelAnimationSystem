using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Data;

// std430 layout
[StructLayout(LayoutKind.Explicit, Size = 96)]
public struct MultiDrawItem
{
    [FieldOffset(0)] public required Vector3 MvpRow1;
    [FieldOffset(16)] public required Vector3 MvpRow2;
    [FieldOffset(32)] public required Vector3 MvpRow3;
    [FieldOffset(48)] public required Vector4 Color1;
    [FieldOffset(64)] public required Vector4 Color2;
    [FieldOffset(80)] public required float Z;
    [FieldOffset(84)] public required int RenderMode;
    [FieldOffset(88)] public required int RenderType;
    [FieldOffset(92)] public required int GlyphOffset;
}