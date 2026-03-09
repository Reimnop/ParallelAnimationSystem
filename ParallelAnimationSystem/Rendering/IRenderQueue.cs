using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.Handle;

namespace ParallelAnimationSystem.Rendering;

public interface IDrawList
{
    CameraState CameraState { get; set; }
    PostProcessingState PostProcessingState { get; set; }
    ColorRgba ClearColor { get; set; }
    
    void AddMesh(MeshHandle mesh, Matrix3x2 transform, ColorRgba color1, ColorRgba color2, RenderMode renderMode);
    void AddText(TextHandle text, Matrix3x2 transform, ColorRgba color);
}

public interface IRenderQueue
{
    MeshHandle CreateMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices);
    void DestroyMesh(MeshHandle handle);
    
    FontHandle CreateFont(int width, int height, ReadOnlySpan<byte> atlas);
    void DestroyFont(FontHandle handle);
    
    TextHandle CreateText(ShapedRichText richText);
    void DestroyText(TextHandle handle);

    IDrawList GetDrawList();
    void SubmitDrawList(IDrawList drawList);
}