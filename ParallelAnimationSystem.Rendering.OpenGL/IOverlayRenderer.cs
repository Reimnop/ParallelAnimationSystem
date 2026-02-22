using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public interface IOverlayRenderer
{
    // Return a texture handle, or 0 to indicate no overlay
    int ProcessFrame(Vector2i size);
}