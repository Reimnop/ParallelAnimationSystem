using System.Diagnostics;
using System.Numerics;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.Data;

namespace ParallelAnimationSystem.Rendering.Common;

public static class RenderUtil
{
    public static float EncodeIntDepth(int depth)
        => depth / 8388608.0f; // 2^23
    
    public static Matrix3x2 GetCameraMatrix(CameraData camera, Vector2i size)
    {
        if (camera.Scale == 0.0f)
            return new Matrix3x2();
        
        var aspectRatio = size.X / (float) size.Y;
        var inversionResult = Matrix3x2.Invert(
            Matrix3x2.CreateScale(Vector2.One * camera.Scale) *
            Matrix3x2.CreateRotation(camera.Rotation) *
            Matrix3x2.CreateTranslation(camera.Position), out var view);
        Debug.Assert(inversionResult);
        
        var projection = Matrix3x2.CreateScale(1.0f / aspectRatio, 1.0f);
        return view * projection;
    }
}