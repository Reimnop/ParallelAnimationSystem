using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Model;

public class BeatmapObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<KeyframeList<Vector2Keyframe>>? PositionKeyframesChanged;
    public event EventHandler<KeyframeList<Vector2Keyframe>>? ScaleKeyframesChanged;
    public event EventHandler<KeyframeList<RotationKeyframe>>? RotationKeyframesChanged;
    public event EventHandler<KeyframeList<BeatmapObjectColorKeyframe>>? ColorKeyframesChanged;

    public string Id { get; }

    public string? ParentId
    {
        get;
        set => SetField(ref field, value);
    }

    public BeatmapObjectType Type
    {
        get;
        set => SetField(ref field, value);
    }
    
    public ParentType ParentType
    {
        get;
        set => SetField(ref field, value);
    }

    public ParentOffset ParentOffset
    {
        get;
        set => SetField(ref field, value);
    }

    public RenderType RenderType
    {
        get;
        set => SetField(ref field, value);
    }

    public Vector2 Origin
    {
        get;
        set => SetField(ref field, value);
    }

    public float RenderDepth
    {
        get;
        set => SetField(ref field, value);
    }

    public AutoKillType AutoKillType
    {
        get;
        set => SetField(ref field, value);
    }

    public float StartTime
    {
        get;
        set => SetField(ref field, value);
    }

    public float AutoKillOffset
    {
        get;
        set => SetField(ref field, value);
    }

    public ObjectShape Shape
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Text
    {
        get;
        set => SetField(ref field, value);
    }
    
    public float Duration => Math.Max(PositionKeyframes.Duration,
        Math.Max(ScaleKeyframes.Duration,
            Math.Max(RotationKeyframes.Duration,
                ColorKeyframes.Duration)));

    public KeyframeList<Vector2Keyframe> PositionKeyframes { get; } = [];
    public KeyframeList<Vector2Keyframe> ScaleKeyframes { get; } = [];
    public KeyframeList<RotationKeyframe> RotationKeyframes { get; } = [];
    public KeyframeList<BeatmapObjectColorKeyframe> ColorKeyframes { get; } = [];

    public BeatmapObject(string id)
    {
        Id = id;
        
        PositionKeyframes.ListChanged += (_, _) => PositionKeyframesChanged?.Invoke(this, PositionKeyframes);
        ScaleKeyframes.ListChanged += (_, _) => ScaleKeyframesChanged?.Invoke(this, ScaleKeyframes);
        RotationKeyframes.ListChanged += (_, _) => RotationKeyframesChanged?.Invoke(this, RotationKeyframes);
        ColorKeyframes.ListChanged += (_, _) => ColorKeyframesChanged?.Invoke(this, ColorKeyframes);
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
}