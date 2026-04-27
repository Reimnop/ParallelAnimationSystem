using System.Numerics;
using System.Runtime.InteropServices;

namespace Tmpx.Demo;

[StructLayout(LayoutKind.Sequential)]
public struct InstanceItem
{
    public Vector2 Position;
    public int ShapeEntryIndex;
}