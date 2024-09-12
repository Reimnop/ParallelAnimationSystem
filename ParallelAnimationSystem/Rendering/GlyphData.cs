using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Rendering;

[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct RenderGlyph(Vector2 min, Vector2 max, Vector2 minUV, Vector2 maxUV, Vector4 color, BoldItalic boldItalic)
{
    public Vector2 Min = min;
    public Vector2 Max = max;
    public Vector2 MinUV = minUV;
    public Vector2 MaxUV = maxUV;
    public Vector4 Color = color;
    public BoldItalic BoldItalic = boldItalic;
}