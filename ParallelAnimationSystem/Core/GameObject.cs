using OpenTK.Mathematics;
using Pamx.Common.Data;
using ParallelAnimationSystem.Core.Animation;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core;

public class GameObject(
    float startTime,
    float killTime,
    Sequence<Vector2, Vector2> positionAnimation,
    Sequence<Vector2, Vector2> scaleAnimation,
    Sequence<float, float> rotationAnimation,
    Sequence<ThemeColor, (Color4, Color4)> themeColorAnimation,
    float parentPositionTimeOffset,
    float parentScaleTimeOffset,
    float parentRotationTimeOffset,
    bool parentAnimatePosition,
    bool parentAnimateScale,
    bool parentAnimateRotation,
    Vector2 origin,
    int shapeIndex,
    int shapeOptionIndex,
    float depth,
    ParentTransform? parent)
{
    public float StartTime { get; } = startTime;
    public float KillTime { get; } = killTime;

    public Sequence<Vector2, Vector2> PositionAnimation { get; } = positionAnimation;
    public Sequence<Vector2, Vector2> ScaleAnimation { get; } = scaleAnimation;
    public Sequence<float, float> RotationAnimation { get; } = rotationAnimation;
    public Sequence<ThemeColor, (Color4, Color4)> ThemeColorAnimation { get; } = themeColorAnimation;
    
    public float ParentPositionTimeOffset { get; } = parentPositionTimeOffset;
    public float ParentScaleTimeOffset { get; } = parentScaleTimeOffset;
    public float ParentRotationTimeOffset { get; } = parentRotationTimeOffset;
    
    public bool ParentAnimatePosition { get; } = parentAnimatePosition;
    public bool ParentAnimateScale { get; } = parentAnimateScale;
    public bool ParentAnimateRotation { get; } = parentAnimateRotation;
    
    public Vector2 Origin { get; } = origin;
    
    public int ShapeIndex { get; } = shapeIndex;
    public int ShapeOptionIndex { get; } = shapeOptionIndex;
    public float Depth { get; } = depth;

    public ParentTransform? Parent { get; } = parent;
    
    public Matrix3 CachedTransform { get; private set; } = Matrix3.Identity;
    public (Color4, Color4) CachedThemeColor { get; private set; } = (Color4.White, Color4.White);

    public Matrix3 CalculateTransform(float time, object? context = null)
    {
        var parentMatrix = Parent is not null
            ? CalculateParentTransform(
                Parent, 
                time, 
                ParentPositionTimeOffset, ParentScaleTimeOffset, ParentRotationTimeOffset, 
                ParentAnimatePosition, ParentAnimateScale, ParentAnimateRotation,
                context)
            : Matrix3.Identity;
        var originMatrix = MathUtil.CreateTranslation(Origin);
        var matrix = 
            MathUtil.CreateScale(ScaleAnimation.Interpolate(time - StartTime, context)) *
            MathUtil.CreateRotation(RotationAnimation.Interpolate(time - StartTime, context)) *
            MathUtil.CreateTranslation(PositionAnimation.Interpolate(time - StartTime, context));
        
        CachedTransform = originMatrix * matrix * parentMatrix;
        return CachedTransform;
    }
    
    public (Color4, Color4) CalculateThemeColor(float time, object? context = null)
    {
        CachedThemeColor = ThemeColorAnimation.Interpolate(time - StartTime, context);
        return CachedThemeColor;
    }
    
    private static Matrix3 CalculateParentTransform(
        ParentTransform transform,
        float time, 
        float positionTimeOffset, float scaleTimeOffset, float rotationTimeOffset,
        bool animatePosition, bool animateScale, bool animateRotation,
        object? context = null)
    {
        var matrix = Matrix3.Identity;
        
        if (animateScale)
        {
            var scale = transform.ScaleAnimation.Interpolate(time + transform.TimeOffset + scaleTimeOffset, context);
            matrix *= MathUtil.CreateScale(scale);
        }
        
        if (animateRotation)
        {
            var rotation = transform.RotationAnimation.Interpolate(time + transform.TimeOffset + rotationTimeOffset, context);
            matrix *= MathUtil.CreateRotation(rotation);
        }
        
        if (animatePosition)
        {
            var position = transform.PositionAnimation.Interpolate(time + transform.TimeOffset + positionTimeOffset, context);
            matrix *= MathUtil.CreateTranslation(position);
        }

        var parent = transform.Parent;
        var parentTransform = parent is not null 
            ? CalculateParentTransform(
                parent, 
                time, 
                transform.ParentPositionTimeOffset, transform.ParentScaleTimeOffset, transform.ParentRotationTimeOffset, 
                transform.ParentAnimatePosition, transform.ParentAnimateScale, transform.ParentAnimateRotation,
                context) 
            : Matrix3.Identity;
        
        return matrix * parentTransform;
    }
}