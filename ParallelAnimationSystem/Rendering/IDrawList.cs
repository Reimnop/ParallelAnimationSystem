using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Rendering.Handle;

namespace ParallelAnimationSystem.Rendering;

public interface IDrawList
{
    CameraState CameraState { get; set; }
    PostProcessingState PostProcessingState { get; set; }
    ColorRgba ClearColor { get; set; }

    void AddMesh(MeshHandle mesh, Matrix3x2 transform, ColorRgba color1, ColorRgba color2, RenderMode renderMode, float gradientRotation, float gradientScale);
    void AddText(TextHandle text, Matrix3x2 transform, ColorRgba color);
}