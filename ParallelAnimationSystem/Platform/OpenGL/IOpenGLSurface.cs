using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Platform.OpenGL;

public interface IOpenGLSurface
{
    Vector2i FramebufferSize { get; }
    bool IsContextLost { get; }

    void MakeContextCurrent();
    void Present(int texture, Vector2i size, ColorRgba clearColor);
}