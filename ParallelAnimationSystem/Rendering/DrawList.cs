using System.Collections;
using OpenTK.Mathematics;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Rendering;

// This should never be instantiated directly, but rather through the Renderer
public class DrawList : IDrawList, IEnumerable<DrawData>
{
    public int Count => count;
    
    public CameraData CameraData { get; set; } = new(Vector2.Zero, 10.0f, 0.0f);
    public PostProcessingData PostProcessingData { get; set; } = default;
    public Color4<Rgba> ClearColor { get; set; } = Color4.Black;
    
    private readonly List<DrawData> drawDataList = [];
    private int count;
    
    public void AddMesh(IMeshHandle mesh, Matrix3 transform, Color4<Rgba> color1, Color4<Rgba> color2, float z, RenderMode renderMode)
    {
        DrawData drawData;
        if (drawDataList.Count > count)
            drawData = drawDataList[count];
        else
        {
            drawData = new DrawData();
            drawDataList.Add(drawData);
        }

        count++;
        
        drawData.RenderType = RenderType.Mesh;
        drawData.Mesh = mesh;
        drawData.Transform = transform;
        drawData.Color1 = color1;
        drawData.Color2 = color2;
        drawData.Z = z;
        drawData.RenderMode = renderMode;
    }
    
    public void AddText(ITextHandle text, Matrix3 transform, Color4<Rgba> color, float z)
    {
        DrawData drawData;
        if (drawDataList.Count > count)
            drawData = drawDataList[count];
        else
        {
            drawData = new DrawData();
            drawDataList.Add(drawData);
        }

        count++;
        
        drawData.RenderType = RenderType.Text;
        drawData.Text = text;
        drawData.Transform = transform;
        drawData.Color1 = color;
        drawData.Z = z;
    }

    public IEnumerator<DrawData> GetEnumerator()
    {
        for (var i = 0; i < count; i++)
            yield return drawDataList[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Reset()
    {
        CameraData = new CameraData(Vector2.Zero, 10.0f, 0.0f);
        PostProcessingData = default;
        ClearColor = Color4.Black;
        count = 0;
    }
}