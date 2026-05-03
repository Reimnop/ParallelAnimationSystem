using System.Numerics;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Rendering.Handle;
using Tmpx.Common;

namespace ParallelAnimationSystem.Rendering;

public interface IRenderingFactory
{
    MeshHandle CreateMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices);
    void DestroyMesh(MeshHandle handle);

    /// <summary>
    /// Replaces the four global font buffers used by the text renderer. FontService calls this once
    /// at startup (and again if fonts ever change) after concatenating all loaded fonts and remapping
    /// per-font-local shape indices to global ones.
    /// </summary>
    void SetFontBuffers(
        ReadOnlySpan<QuadraticCurve> curves,
        ReadOnlySpan<int> curveIndices,
        ReadOnlySpan<BandEntry> bandEntries,
        ReadOnlySpan<ShapeEntry> shapeEntries);

    TextHandle CreateText(ShapedRichText richText);
    void DestroyText(TextHandle handle);
}
