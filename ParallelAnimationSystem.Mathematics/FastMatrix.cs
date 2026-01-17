using System.Numerics;
using System.Runtime.CompilerServices;

namespace ParallelAnimationSystem.Mathematics;

public static class FastMatrix
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 GetTranslationMatrix(float tx, float ty)
        => new(
            1f, 0f,
            0f, 1f,
            tx, ty);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 GetScaleMatrix(float sx, float sy)
        => new(
            sx, 0f,
            0f, sy,
            0f, 0f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 GetRotationMatrix(float phi)
    {
        var (sin, cos) = MathF.SinCos(phi);
        
        return new Matrix3x2(
            cos, sin,
            -sin, cos,
            0f, 0f);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 GetTranslationScaleMatrix(float tx, float ty, float sx, float sy)
        => new(
            sx, 0f,
            0f, sy,
            tx, ty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 GetTranslationRotationMatrix(float tx, float ty, float phi)
    {
        var (sin, cos) = MathF.SinCos(phi);
        
        return new Matrix3x2(
            cos, sin,
            -sin, cos,
            tx, ty);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 GetRotationScaleMatrix(float sx, float sy, float phi)
    {
        var (sin, cos) = MathF.SinCos(phi);
        
        return new Matrix3x2(
            cos * sx, sin * sx,
            -sin * sy, cos * sy,
            0f, 0f);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x2 GetTranslationRotationScaleMatrix(float tx, float ty, float sx, float sy, float phi)
    {
        var (sin, cos) = MathF.SinCos(phi);
        
        return new Matrix3x2(
            cos * sx, sin * sx,
            -sin * sy, cos * sy,
            tx, ty);
    }
}