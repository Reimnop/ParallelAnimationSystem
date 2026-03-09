using System.Diagnostics;
using System.Numerics;
using ParallelAnimationSystem.Mathematics;
using ParallelAnimationSystem.Rendering.Data;

namespace ParallelAnimationSystem.Rendering;

public static class RenderUtil
{
    public static Matrix3x2 GetCameraMatrix(in CameraState camera, Vector2i size)
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
    
    public static bool ShouldUseTransparentDrawList(in DrawCommand drawCommand, in DrawData drawData)
    {
        if (drawCommand.DrawType == DrawType.Text)
            return true; // Text is always transparent
        
        ref var meshDrawItem = ref drawData.MeshDrawItems[drawCommand.DrawId];
        if (meshDrawItem.RenderMode == RenderMode.Normal)
            return meshDrawItem.Color1.A < 1f; // We only use Color1 in normal render mode

        return meshDrawItem.Color1.A < 1f || meshDrawItem.Color2.A < 1f;
    }
}