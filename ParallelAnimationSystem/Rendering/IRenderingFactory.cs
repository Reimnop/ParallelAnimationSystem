using System.Numerics;
using ParallelAnimationSystem.Text;

namespace ParallelAnimationSystem.Rendering;

public interface IRenderingFactory
{
    IMesh CreateMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices);
    IFont CreateFont(Stream stream);
    IText CreateText(ShapedRichText richText);
    IDrawList CreateDrawList();
}