using System.Numerics;
using System.Runtime.InteropServices;

namespace ParallelAnimationSystem.Core.Data;

[StructLayout(LayoutKind.Sequential, Size = 64)] // fits in exactly one 64-byte cache line
public struct ObjectDrawItem
{
    public ulong SortKeyPrimary; // 8 bytes
    public ulong SortKeySecondary; // 8 bytes
    public Matrix3x2 Transform; // 24 bytes
    public uint Color1; // 4 bytes
    public uint Color2; // 4 bytes
    public float Opacity; // 4 bytes
    public int ObjectIndex; // 4 bytes
}
