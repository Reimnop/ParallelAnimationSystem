using System.Numerics;
using ParallelAnimationSystem.Rendering.Handle;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public class BuiltinRenderingPrimitive : IDisposable
{
    public MeshHandle GlyphMeshHandle { get; }

    private readonly IRenderingFactory renderingFactory;
    
    public BuiltinRenderingPrimitive(IRenderingFactory renderingFactory)
    {
        this.renderingFactory = renderingFactory;
        
        Vector2[] vertices =
        [
            new(0f, 1f),
            new(1f, 1f),
            new(0f, 0f),
            new(1f, 0f)
        ];
            
        int[] indices =
        [
            0, 1, 2,
            3, 2, 1
        ];
            
        GlyphMeshHandle = renderingFactory.CreateMesh(vertices, indices);
    }

    public void Dispose()
    {
        renderingFactory.DestroyMesh(GlyphMeshHandle);
    }
}