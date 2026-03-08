namespace ParallelAnimationSystem.Rendering.Handle;

public readonly struct TextHandle(int id): IRenderingResourceHandle
{
    public int Id => id;
}