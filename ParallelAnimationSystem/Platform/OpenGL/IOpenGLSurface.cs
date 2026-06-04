using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Platform.OpenGL;

public interface IOpenGLSurface
{
    Vector2i RenderSize { get; }
    bool IsContextLost { get; }
    
    void Present(int texture, Vector2i size, ColorRgba clearColor);
}