using ParallelAnimationSystem.Windowing;

namespace ParallelAnimationSystem.Rendering;

public interface IRenderer
{
    IWindow Window { get; }

    void ProcessFrame(IDrawList drawList);
}