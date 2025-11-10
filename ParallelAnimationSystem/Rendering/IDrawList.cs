using OpenTK.Mathematics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Rendering;

public interface IDrawList
{
    CameraData CameraData { get; set; }
    PostProcessingData PostProcessingData { get; set; }
    ColorRgba ClearColor { get; set; }
    
    void AddMesh(IMeshHandle mesh, Matrix3 transform, ColorRgba color1, ColorRgba color2, RenderMode renderMode);
    void AddText(ITextHandle text, Matrix3 transform, ColorRgba color);
}