using System.ComponentModel;
using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using Pamx.Common.Data;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Animation;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Beatmap;

public class BeatmapObjectData(
    IEnumerable<Animation.Keyframe<Vector2>> positionKeyframes,
    IEnumerable<Animation.Keyframe<Vector2>> scaleKeyframes,
    IEnumerable<Animation.Keyframe<float>> rotationKeyframes,
    IEnumerable<Animation.Keyframe<ThemeColor>> themeColorKeyframes)
    : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ParentTemporalOffsets ParentTemporalOffsets
    {
        get => parentTemporalOffsets;
        set => SetField(ref parentTemporalOffsets, value);
    }

    public ParentTypes ParentTypes
    {
        get => parentTypes;
        set => SetField(ref parentTypes, value);
    }
    
    public RenderMode RenderMode
    {
        get => renderMode;
        set => SetField(ref renderMode, value);
    }

    public Vector2 Origin
    {
        get => origin;
        set => SetField(ref origin, value);
    }

    public int RenderDepth
    {
        get => renderDepth;
        set => SetField(ref renderDepth, value);
    }
    
    public AutoKillType AutoKillType
    {
        get => autoKillType;
        set => SetField(ref autoKillType, value);
    }
    
    public float StartTime
    {
        get => startTime;
        set => SetField(ref startTime, value);
    }
    
    public float KillTimeOffset
    {
        get => killTimeOffset;
        set => SetField(ref killTimeOffset, value);
    }
    
    // "Shape" in PA
    public int ShapeCategoryIndex
    {
        get => shapeCategoryIndex;
        set => SetField(ref shapeCategoryIndex, value);
    }
    
    // "Shape Option" in PA
    public int ShapeIndex
    {
        get => shapeIndex;
        set => SetField(ref shapeIndex, value);
    }

    public Sequence<Vector2, Vector2> PositionSequence { get; } = new(positionKeyframes, InterpolateVector2);
    public Sequence<Vector2, Vector2> ScaleSequence { get; } = new(scaleKeyframes, InterpolateVector2);
    public Sequence<float, float> RotationSequence { get; } = new(rotationKeyframes, InterpolateFloat);
    public Sequence<ThemeColor, (Color4<Rgba>, Color4<Rgba>)> ThemeColorSequence { get; } = new(themeColorKeyframes, InterpolateThemeColor);

    private ParentTemporalOffsets parentTemporalOffsets = new(0f, 0f, 0f);
    private ParentTypes parentTypes = new(true, false, true);
    
    private RenderMode renderMode = RenderMode.Normal;
    
    private Vector2 origin = new(0.5f);

    private int renderDepth = 20;

    private AutoKillType autoKillType = AutoKillType.FixedTime;
    private float startTime = 0f;
    private float killTimeOffset = 0f;

    private int shapeCategoryIndex = 0;
    private int shapeIndex = 0;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    
    private static Vector2 InterpolateVector2(Vector2 a, Vector2 b, float t, object? context)
        => new(
            MathUtil.Lerp(a.X, b.X, t),
            MathUtil.Lerp(a.Y, b.Y, t));
    
    private static float InterpolateFloat(float a, float b, float t, object? context)
        => MathUtil.Lerp(a, b, t);
    
    private static (Color4<Rgba>, Color4<Rgba>) InterpolateThemeColor(ThemeColor a, ThemeColor b, float t, object? context)
    {
        if (context is not ThemeColors colors)
            throw new ArgumentException($"Context is not of type {typeof(ThemeColors)}");

        var opacityA = a.Opacity;
        var opacityB = b.Opacity;
        var colorAStart = colors.Object[a.Index];
        var colorAEnd = colors.Object[a.EndIndex];
        var colorBStart = colors.Object[b.Index];
        var colorBEnd = colors.Object[b.EndIndex];

        var opacity = MathUtil.Lerp(opacityA, opacityB, t);
        var color1 = new Color4<Rgba>(
            MathUtil.Lerp(colorAStart.X, colorBStart.X, t),
            MathUtil.Lerp(colorAStart.Y, colorBStart.Y, t),
            MathUtil.Lerp(colorAStart.Z, colorBStart.Z, t),
            opacity);
        var color2 = new Color4<Rgba>(
            MathUtil.Lerp(colorAEnd.X, colorBEnd.X, t),
            MathUtil.Lerp(colorAEnd.Y, colorBEnd.Y, t),
            MathUtil.Lerp(colorAEnd.Z, colorBEnd.Z, t),
            opacity);
        return (color1, color2);
    }
}