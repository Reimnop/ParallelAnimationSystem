using System.Numerics;
using System.Runtime.InteropServices;

namespace Tmpx.Demo;

[StructLayout(LayoutKind.Sequential)]
public struct InstanceItem
{
    public Matrix3x2 Transform;
    public int ShapeEntryIndex;
}