using System.Collections;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Data;

public class DrawList : IEnumerable<DrawData>
{
    public int Count => drawData.Count;
    
    public CameraData CameraData { get; set; } = new(Vector2.Zero, 10.0f, 0.0f);
    public PostProcessingData PostProcessingData { get; set; } = default;
    public Color4 ClearColor { get; set; } = Color4.Black;
    
    private readonly List<DrawData> drawData = [];
    
    public void AddMesh(MeshHandle mesh, Matrix3 transform, float z, Color4 color)
    {
        drawData.Add(new DrawData(mesh, transform, z, color));
    }

    public IEnumerator<DrawData> GetEnumerator()
    {
        return drawData.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}