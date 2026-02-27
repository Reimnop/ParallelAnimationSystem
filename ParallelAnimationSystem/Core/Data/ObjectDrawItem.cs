using System.Numerics;
using System.Runtime.InteropServices;

namespace ParallelAnimationSystem.Core.Data;

[StructLayout(LayoutKind.Sequential, Size = 64)] // fits in exactly one 64-byte cache line
public struct ObjectDrawItem
{
    public uint SortKey; // 4 bytes
    public Matrix3x2 Transform; // 24 bytes
    public ColorRgb Color1; // 12 bytes
    public ColorRgb Color2; // 12 bytes
    public float Opacity; // 4 bytes
    public int ObjectIndex; // 4 bytes
}
