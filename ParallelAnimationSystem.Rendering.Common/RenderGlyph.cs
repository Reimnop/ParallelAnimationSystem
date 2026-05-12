using System.Numerics;
using System.Runtime.InteropServices;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Rendering.Common;

[StructLayout(LayoutKind.Sequential)]
public struct RenderGlyph
{
    public Matrix3x2 Transform; // 0 - 24 (6 floats)
    public ColorRgba Color; // 24 - 40 (4 floats)
    public int ShapeEntryIndex; // 40 - 44
}
