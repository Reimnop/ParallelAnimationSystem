using OpenTK.Mathematics;
using Pamx.Common.Data;
using ParallelAnimationSystem.Core.Animation;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Rendering.TextProcessing;
using ParallelAnimationSystem.Util;
using TmpParser;

namespace ParallelAnimationSystem.Core;

public class GameObject(
    float startTime,
    float killTime,
    Sequence<Vector2, Vector2> positionAnimation,
    Sequence<Vector2, Vector2> scaleAnimation,
    Sequence<float, float> rotationAnimation,
    Sequence<ThemeColor, (Color4<Rgba>, Color4<Rgba>)> themeColorAnimation,
    float parentPositionTimeOffset,
    float parentScaleTimeOffset,
    float parentRotationTimeOffset,
    bool parentAnimatePosition,
    bool parentAnimateScale,
    bool parentAnimateRotation,
    RenderMode renderMode,
    Vector2 origin,
    int shapeIndex,
    int shapeOptionIndex,
    int renderDepth,
    int parentDepth,
    int depthTiebreaker, // should be set to the index of the object in the beatmap
    string? text,
    HorizontalAlignment horizontalAlignment,
    VerticalAlignment verticalAlignment,
    List<ParentTransform> parents)
{
    private static readonly Matrix3 TextScale = MathUtil.CreateScale(Vector2.One * 3.0f / 32.0f);
    
    public float StartTime { get; } = startTime;
    public float KillTime { get; } = killTime;

    public Sequence<Vector2, Vector2> PositionAnimation { get; } = positionAnimation;
    public Sequence<Vector2, Vector2> ScaleAnimation { get; } = scaleAnimation;
    public Sequence<float, float> RotationAnimation { get; } = rotationAnimation;
    public Sequence<ThemeColor, (Color4<Rgba>, Color4<Rgba>)> ThemeColorAnimation { get; } = themeColorAnimation;
    
    public float ParentPositionTimeOffset { get; } = parentPositionTimeOffset;
    public float ParentScaleTimeOffset { get; } = parentScaleTimeOffset;
    public float ParentRotationTimeOffset { get; } = parentRotationTimeOffset;
    
    public bool ParentAnimatePosition { get; } = parentAnimatePosition;
    public bool ParentAnimateScale { get; } = parentAnimateScale;
    public bool ParentAnimateRotation { get; } = parentAnimateRotation;
    
    public RenderMode RenderMode { get; } = renderMode;
    
    public Vector2 Origin { get; } = origin;
    
    public int ShapeIndex { get; } = shapeIndex;
    public int ShapeOptionIndex { get; } = shapeOptionIndex;

    public int RenderDepth { get; } = renderDepth;
    public int ParentDepth { get; } = parentDepth;
    public int DepthTiebreaker { get; } = depthTiebreaker;
    
    public string? Text { get; } = text;
    public HorizontalAlignment HorizontalAlignment { get; } = horizontalAlignment;
    public VerticalAlignment VerticalAlignment { get; } = verticalAlignment;

    public List<ParentTransform> Parents { get; } = parents;
    
    public Matrix3 CachedTransform { get; private set; } = Matrix3.Identity;
    public (Color4<Rgba>, Color4<Rgba>) CachedThemeColor { get; private set; } = (Color4.White, Color4.White);

    public Matrix3 CalculateTransform(float time, object? context = null)
    {
        var parentMatrix = Parents.Count > 0
            ? CalculateParentTransform(
                Parents, 
                time, 
                ParentPositionTimeOffset, ParentScaleTimeOffset, ParentRotationTimeOffset, 
                ParentAnimatePosition, ParentAnimateScale, ParentAnimateRotation,
                context)
            : Matrix3.Identity;
        
        var originMatrix = MathUtil.CreateTranslation(Origin);
        
        // Apply text scale if the shape is text
        var additionalScale = ShapeIndex == 4 ? TextScale : Matrix3.Identity;
        
        var matrix = 
            additionalScale *
            MathUtil.CreateScale(ScaleAnimation.Interpolate(time - StartTime, context)) *
            MathUtil.CreateRotation(RotationAnimation.Interpolate(time - StartTime, context)) *
            MathUtil.CreateTranslation(PositionAnimation.Interpolate(time - StartTime, context));
        
        CachedTransform = originMatrix * matrix * parentMatrix;
        return CachedTransform;
    }
    
    public (Color4<Rgba>, Color4<Rgba>) CalculateThemeColor(float time, object? context = null)
    {
        CachedThemeColor = ThemeColorAnimation.Interpolate(time - StartTime, context);
        return CachedThemeColor;
    }
    
    private static Matrix3 CalculateParentTransform(
        List<ParentTransform> parents,
        float time, 
        float positionTimeOffset, float scaleTimeOffset, float rotationTimeOffset,
        bool animatePosition, bool animateScale, bool animateRotation,
        object? context = null)
    {
        var matrix = Matrix3.Identity;

        foreach (var parent in parents)
        {
            var currentMatrix = Matrix3.Identity;
            
            if (animateScale)
            {
                var scale = parent.ScaleAnimation.Interpolate(time + parent.TimeOffset + scaleTimeOffset, context);
                currentMatrix *= MathUtil.CreateScale(scale);
            }
        
            if (animateRotation)
            {
                var rotation = parent.RotationAnimation.Interpolate(time + parent.TimeOffset + rotationTimeOffset, context);
                currentMatrix *= MathUtil.CreateRotation(rotation);
            }
        
            if (animatePosition)
            {
                var position = parent.PositionAnimation.Interpolate(time + parent.TimeOffset + positionTimeOffset, context);
                currentMatrix *= MathUtil.CreateTranslation(position);
            }
            
            positionTimeOffset = parent.ParentPositionTimeOffset;
            scaleTimeOffset = parent.ParentScaleTimeOffset;
            rotationTimeOffset = parent.ParentRotationTimeOffset;
            
            animatePosition = parent.ParentAnimatePosition;
            animateScale = parent.ParentAnimateScale;
            animateRotation = parent.ParentAnimateRotation;
            
            matrix *= currentMatrix;
        }
        
        return matrix;
    }
}