using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.Handle;
using Tmpx.Common;

namespace ParallelAnimationSystem.Rendering;

public interface IDrawList
{
    CameraState CameraState { get; set; }
    PostProcessingState PostProcessingState { get; set; }
    ColorRgba ClearColor { get; set; }

    void AddMesh(MeshHandle mesh, Matrix3x2 transform, ColorRgba color1, ColorRgba color2, RenderMode renderMode, float gradientRotation, float gradientScale);
    void AddText(TextHandle text, Matrix3x2 transform, ColorRgba color);
}

public interface IRenderQueue
{
    MeshHandle CreateMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices);
    void DestroyMesh(MeshHandle handle);

    /// <summary>
    /// Replaces the four global font buffers. See <see cref="IRenderingFactory.SetFontBuffers"/>.
    /// </summary>
    void SetFontBuffers(
        ReadOnlySpan<QuadraticCurve> curves,
        ReadOnlySpan<int> curveIndices,
        ReadOnlySpan<BandEntry> bandEntries,
        ReadOnlySpan<ShapeEntry> shapeEntries);

    TextHandle CreateText(ShapedRichText richText);
    void DestroyText(TextHandle handle);

    IDrawList GetDrawList();
    void SubmitDrawList(IDrawList drawList);
}
