using System.Numerics;
using ParallelAnimationSystem.Core.Text;
using ParallelAnimationSystem.Rendering.Handle;

namespace ParallelAnimationSystem.Rendering;

public interface IRenderingFactory
{
    MeshHandle CreateMesh(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices);
    void DestroyMesh(MeshHandle handle);
    
    FontHandle CreateFont(int width, int height, ReadOnlySpan<byte> atlas);
    void DestroyFont(FontHandle handle);
    
    TextHandle CreateText(ShapedRichText richText);
    void DestroyText(TextHandle handle);
}