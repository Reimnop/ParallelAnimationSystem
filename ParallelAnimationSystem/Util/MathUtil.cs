using OpenTK.Mathematics;

namespace ParallelAnimationSystem.Util;

public static class MathUtil
{
    public static Matrix3 CreateTranslation(Vector2 translation)
        => new(
            1.0f,          0.0f,          0.0f,
            0.0f,          1.0f,          0.0f,
            translation.X, translation.Y, 1.0f);
    
    public static Matrix3 CreateScale(Vector2 scale)
        => new(
            scale.X, 0.0f,    0.0f,
            0.0f,    scale.Y, 0.0f,
            0.0f,    0.0f,    1.0f);
    
    public static Matrix3 CreateRotation(float angle)
    {
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        
        return new Matrix3(
             cos, sin,  0.0f,
            -sin, cos,  0.0f,
            0.0f, 0.0f, 1.0f);
    }
    
    public static int DivideCeil(int dividend, int divisor)
        => (dividend + divisor - 1) / divisor;
    
    public static double Lerp(double start, double end, double t)
        => start + t * (end - start);
    
    public static float Lerp(float start, float end, float t)
        => start + t * (end - start);
}