using OpenTK.Mathematics;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Data;

public class DrawData
{
    public RenderType RenderType { get; set; }
    public IMeshHandle? Mesh { get; set; }
    public ITextHandle? Text { get; set; }
    public Matrix3 Transform { get; set; }
    public Color4<Rgba> Color1 { get; set; }
    public Color4<Rgba> Color2 { get; set; }
    public float Z { get; set; }
    public RenderMode RenderMode { get; set; }
}