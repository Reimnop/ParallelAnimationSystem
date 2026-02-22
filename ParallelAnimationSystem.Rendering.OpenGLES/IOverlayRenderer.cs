using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Rendering.OpenGLES;

public interface IOverlayRenderer
{
    // Return a texture handle, or 0 to indicate no overlay
    int ProcessFrame(Vector2i size);
}