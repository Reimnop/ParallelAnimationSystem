using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Numerics;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Animation;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Data;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Core.Beatmap;

public class BeatmapObject(
    ObjectId id,
    IEnumerable<PositionScaleKeyframe> positionKeyframes,
    IEnumerable<PositionScaleKeyframe> scaleKeyframes,
    IEnumerable<RotationKeyframe> rotationKeyframes,
    IEnumerable<BeatmapObjectColorKeyframe> themeColorKeyframes)
    : IIndexedObject, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public ObjectId Id { get; } = id;
    
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

    public ParentOffset ParentOffset
    {
        get => parentOffset;
        set => SetField(ref parentOffset, value);
    }

    public ParentType ParentType
    {
        get => parentType;
        set => SetField(ref parentType, value);
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

    public float RenderDepth
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
    public ObjectShape Shape
    {
        get => shape;
        set => SetField(ref shape, value);
    }

    public string? Text
    {
        get => text;
        set => SetField(ref text, value);
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

    private ParentOffset parentOffset = new(0f, 0f, 0f);
    private ParentType parentType = ParentType.Position | ParentType.Scale | ParentType.Rotation;
    
    private RenderMode renderMode = RenderMode.Normal;
    
    private Vector2 origin = new(0.5f);

    private float renderDepth = 20f;

    private AutoKillType autoKillType = AutoKillType.FixedTime;
    private float startTime = 0f;
    private float killTimeOffset = 0f;

    private ObjectShape shape = ObjectShape.SquareSolid;
    private string? text = null;

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