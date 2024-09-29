using OpenTK.Mathematics;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Rendering;

public interface IDrawList
{
    CameraData CameraData { get; set; }
    PostProcessingData PostProcessingData { get; set; }
    Color4<Rgba> ClearColor { get; set; }
    
    void AddMesh(IMeshHandle mesh, Matrix3 transform, Color4<Rgba> color1, Color4<Rgba> color2, float z, RenderMode renderMode);
    void AddText(ITextHandle text, Matrix3 transform, Color4<Rgba> color, float z);
}