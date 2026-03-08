namespace ParallelAnimationSystem.Rendering.Handle;

public readonly struct FontHandle(int id) : IRenderingResourceHandle
{
    public int Id => id;
}