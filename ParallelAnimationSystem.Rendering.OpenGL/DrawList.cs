using System.Collections;
using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Rendering.OpenGL;

public class DrawList : IDrawList, IReadOnlyCollection<DrawList.DrawData>
{
    public class DrawData
    {
        public RenderType RenderType { get; set; }
        public Renderer.MeshHandle? Mesh { get; set; }
        public Renderer.TextHandle? Text { get; set; }
        public Matrix3x2 Transform { get; set; }
        public ColorRgba Color1 { get; set; }
        public ColorRgba Color2 { get; set; }
        public RenderMode RenderMode { get; set; }
    
        public int Index { get; set; }
    }
    
    public int Count => count;
    
    public CameraData CameraData { get; set; } = new(Vector2.Zero, 10.0f, 0.0f);
    public PostProcessingData PostProcessingData { get; set; } = default;
    public ColorRgba ClearColor { get; set; } = new(0.0f, 0.0f, 0.0f, 1.0f);
    
    private readonly List<DrawData> drawDataList = [];
    private int count;
    
    public void AddMesh(IMeshHandle mesh, Matrix3x2 transform, ColorRgba color1, ColorRgba color2, RenderMode renderMode)
    {
        if (mesh is not Renderer.MeshHandle rendererMesh)
            throw new ArgumentException("Invalid mesh handle type", nameof(mesh));
        
        DrawData drawData;
        if (drawDataList.Count > count)
            drawData = drawDataList[count];
        else
        {
            drawData = new DrawData();
            drawDataList.Add(drawData);
        }
        
        drawData.RenderType = RenderType.Mesh;
        drawData.Mesh = rendererMesh;
        drawData.Transform = transform;
        drawData.Color1 = color1;
        drawData.Color2 = color2;
        drawData.RenderMode = renderMode;
        drawData.Index = count;
        
        count++;
    }
    
    public void AddText(ITextHandle text, Matrix3x2 transform, ColorRgba color)
    {
        if (text is not Renderer.TextHandle rendererText)
            throw new ArgumentException("Invalid text handle type", nameof(text));
        
        DrawData drawData;
        if (drawDataList.Count > count)
            drawData = drawDataList[count];
        else
        {
            drawData = new DrawData();
            drawDataList.Add(drawData);
        }
        
        drawData.RenderType = RenderType.Text;
        drawData.Text = rendererText;
        drawData.Transform = transform;
        drawData.Color1 = color;
        drawData.Index = count;
        
        count++;
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
        ClearColor = new ColorRgba(0.0f, 0.0f, 0.0f, 1.0f);
        count = 0;
    }
}