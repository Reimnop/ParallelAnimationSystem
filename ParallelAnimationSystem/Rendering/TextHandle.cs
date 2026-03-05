namespace ParallelAnimationSystem.Rendering;

public readonly struct TextHandle(int id): IRenderingResourceHandle
{
    public int Id => id;
}