namespace ParallelAnimationSystem.Rendering;

public interface IRenderer
{
    void ProcessFrame(DrawList drawList);
}