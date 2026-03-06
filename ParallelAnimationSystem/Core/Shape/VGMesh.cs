using System.Numerics;

namespace ParallelAnimationSystem.Core.Shape;

public class VGMesh(Vector2[] vertices, int[] indices)
{
    public Vector2[] Vertices => vertices;
    public int[] Indices => indices;
}