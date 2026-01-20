using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public interface IOverlayRenderer
{
    // texture is an OpenGL texture handle, in RGBA16F format
    // Draw into the texture of given size
    void ProcessFrame(int texture, Vector2i size);
}