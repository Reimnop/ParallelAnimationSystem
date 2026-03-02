using System.Numerics;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public class Mesh(int id, Vector2[] vertices, int[] indices) : IMesh
{
    public int Id => id;
    public Vector2[] Vertices => vertices;
    public int[] Indices => indices;
}