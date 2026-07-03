using System.Numerics;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Rendering.Handle;
using Tmpx.Common;

namespace ParallelAnimationSystem.Rendering;

public interface IRenderingFactory
{
    MeshHandle CreateMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices);
    void DestroyMesh(MeshHandle handle);
    void SetFontBuffers(
        ReadOnlySpan<QuadraticCurve> curves, 
        ReadOnlySpan<int> curveIndices, 
        ReadOnlySpan<BandEntry> bandEntries, 
        ReadOnlySpan<ShapeEntry> shapeEntries);
    TextHandle CreateText(ShapedRichText richText);
    void DestroyText(TextHandle handle);
}
