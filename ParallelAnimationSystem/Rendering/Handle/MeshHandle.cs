namespace ParallelAnimationSystem.Rendering.Handle;

public readonly struct MeshHandle(int id) : IRenderingResourceHandle
{
    public int Id => id;
}