using System.Numerics;
using System.Runtime.CompilerServices;

namespace ParallelAnimationSystem.Mathematics;

public static class MathUtil
{
    public static int DivideCeil(int dividend, int divisor)
        => (dividend + divisor - 1) / divisor;

    public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        => new(
            float.Lerp(a.X, b.X, t),
            float.Lerp(a.Y, b.Y, t));

    public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        => new(
            float.Lerp(a.X, b.X, t),
            float.Lerp(a.Y, b.Y, t),
            float.Lerp(a.Z, b.Z, t));
    
    public static Vector4 Lerp(Vector4 a, Vector4 b, float t)
        => new(
            float.Lerp(a.X, b.X, t),
            float.Lerp(a.Y, b.Y, t),
            float.Lerp(a.Z, b.Z, t),
            float.Lerp(a.W, b.W, t));
    
    public static float InverseLerp(float a, float b, float value)
        => (value - a) / (b - a);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DegreesToRadians(float degree)
        => degree * (MathF.PI / 180f);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float RadiansToDegrees(float radian)
        => radian * (180f / MathF.PI);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float MapRange(float value, float inMin, float inMax, float outMin, float outMax)
        => outMin + (value - inMin) * (outMax - outMin) / (inMax - inMin);
}