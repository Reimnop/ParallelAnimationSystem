namespace ParallelAnimationSystem.Rendering;

public interface IRenderer
{
    void ProcessFrame(IDrawList drawList);
}