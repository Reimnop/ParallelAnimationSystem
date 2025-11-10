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
    
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        => new(
            Lerp(a.X, b.X, t),
            Lerp(a.Y, b.Y, t));
    
    public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        => new(
            Lerp(a.X, b.X, t),
            Lerp(a.Y, b.Y, t),
            Lerp(a.Z, b.Z, t));
    
    public static Vector4 Lerp(Vector4 a, Vector4 b, float t)
        => new(
            Lerp(a.X, b.X, t),
            Lerp(a.Y, b.Y, t),
            Lerp(a.Z, b.Z, t),
            Lerp(a.W, b.W, t));
    
    public static float InverseLerp(float a, float b, float value)
        => (value - a) / (b - a);
}