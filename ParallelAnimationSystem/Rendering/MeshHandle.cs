namespace ParallelAnimationSystem.Rendering;

public readonly struct MeshHandle(int id) : IRenderingResourceHandle
{
    public int Id => id;
}