using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Data;

namespace ParallelAnimationSystem.Rendering;

public interface IDrawList
{
    CameraData CameraData { get; set; }
    PostProcessingData PostProcessingData { get; set; }
    ColorRgba ClearColor { get; set; }
    
    void AddMesh(IMesh mesh, Matrix3x2 transform, ColorRgba color1, ColorRgba color2, RenderMode renderMode);
    void AddText(IText text, Matrix3x2 transform, ColorRgba color);
    void Clear();
}