using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Rendering;

namespace ParallelAnimationSystem.Data;

public class DrawData
{
    public RenderType RenderType { get; set; }
    public IMeshHandle? Mesh { get; set; }
    public ITextHandle? Text { get; set; }
    public Matrix3x2 Transform { get; set; }
    public ColorRgba Color1 { get; set; }
    public ColorRgba Color2 { get; set; }
    public RenderMode RenderMode { get; set; }
    
    public int Index { get; set; }
}