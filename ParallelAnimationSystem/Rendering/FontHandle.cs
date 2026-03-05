namespace ParallelAnimationSystem.Rendering;

public readonly struct FontHandle(int id) : IRenderingResourceHandle
{
    public int Id => id;
}