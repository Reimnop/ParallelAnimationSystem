using System.Numerics;

namespace ParallelAnimationSystem.Rendering.Common;

public class Mesh(Vector2[] vertices, int[] indices)
{
    public Vector2[] Vertices => vertices;
    public int[] Indices => indices;
}