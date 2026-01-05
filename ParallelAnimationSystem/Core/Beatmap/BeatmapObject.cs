using System.ComponentModel;
using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Animation;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Beatmap;

public class BeatmapObject(
    BeatmapObjectId id,
    IEnumerable<PositionScaleKeyframe> positionKeyframes,
    IEnumerable<PositionScaleKeyframe> scaleKeyframes,
    IEnumerable<RotationKeyframe> rotationKeyframes,
    IEnumerable<BeatmapObjectColorKeyframe> themeColorKeyframes)
    : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public BeatmapObjectId Id { get; } = id;
    
    public string Name
    {
        get => name;
        set => SetField(ref name, value);
    }

    public bool IsEmpty
    {
        get => isEmpty;
        set => SetField(ref isEmpty, value);
    }

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

    public Sequence<PositionScaleKeyframe, object?, Vector2> PositionSequence { get; } = new(
        positionKeyframes,
        PositionScaleKeyframe.ResolveToValue,
        MathUtil.Lerp);
    
    public Sequence<PositionScaleKeyframe, object?, Vector2> ScaleSequence { get; } = new(
        scaleKeyframes,
        PositionScaleKeyframe.ResolveToValue,
        MathUtil.Lerp);
    
    public Sequence<RotationKeyframe, object?, float> RotationSequence { get; } = new(
        rotationKeyframes,
        RotationKeyframe.ResolveToValue,
        MathUtil.Lerp);
    
    public Sequence<BeatmapObjectColorKeyframe, ThemeColorState, BeatmapObjectColor> ThemeColorSequence { get; } = new(
        themeColorKeyframes,
        BeatmapObjectColorKeyframe.ResolveToValue,
        BeatmapObjectColor.Lerp);

    private string name = string.Empty;

    private bool isEmpty = true;

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

    public float CalculateKillTime(float startTimeOffset)
    {
        var actualStartTime = StartTime + startTimeOffset;
        
        return AutoKillType switch
        {
            AutoKillType.FixedTime => actualStartTime + KillTimeOffset,
            AutoKillType.NoAutoKill => float.PositiveInfinity,
            AutoKillType.LastKeyframe => actualStartTime + GetObjectLength(),
            AutoKillType.LastKeyframeOffset => actualStartTime + GetObjectLength() + KillTimeOffset,
            AutoKillType.SongTime => KillTimeOffset,
            _ => throw new ArgumentOutOfRangeException(nameof(AutoKillType), $"Unknown AutoKillType '{AutoKillType}'!")
        };
    }

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
    
    private float GetObjectLength()
        => Math.Max(PositionSequence.Length, Math.Max(ScaleSequence.Length, Math.Max(RotationSequence.Length, ThemeColorSequence.Length)));
}