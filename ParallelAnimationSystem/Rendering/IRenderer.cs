namespace ParallelAnimationSystem.Rendering;

public interface IRenderer
{
    void ProcessFrame(IDrawDataProvider drawDataProvider);
}