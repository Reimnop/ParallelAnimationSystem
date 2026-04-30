using System.Numerics;
using System.Runtime.InteropServices;
using Tmpx.Common;

namespace Tmpx.Demo;

[StructLayout(LayoutKind.Sequential)]
public struct InstanceItem
{
    public Matrix3x2 Transform;
    public Color Color;
    public int ShapeEntryIndex;
}