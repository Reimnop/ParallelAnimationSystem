using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using Pamx.Objects;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Shape;

namespace ParallelAnimationSystem.Core.Model;

public class BeatmapObject : IStringIdentifiable, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<KeyframeList<RandomizableKeyframe<Vector2>>>? PositionKeyframesChanged;
    public event EventHandler<KeyframeList<RandomizableKeyframe<Vector2>>>? ScaleKeyframesChanged;
    public event EventHandler<KeyframeList<RandomizableKeyframe<float>>>? RotationKeyframesChanged;
    public event EventHandler<KeyframeList<Keyframe<BeatmapObjectIndexedColor>>>? ColorKeyframesChanged;

    public string Id { get; }

    public string Name
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

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

    public GradientType RenderType
    {
        get;
        set => SetField(ref field, value);
    }
    
    public float GradientRotation
    {
        get;
        set => SetField(ref field, value);
    }
    
    public float GradientScale
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
    
    public float StartTime
    {
        get;
        set => SetField(ref field, value);
    }

    public AutoKillType AutoKillType
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
    
    public VGShapeInfo? CustomShapeInfo
    {
        get;
        set => SetField(ref field, value);
    }
    
    public float Duration => Math.Max(PositionKeyframes.Duration,
        Math.Max(ScaleKeyframes.Duration,
            Math.Max(RotationKeyframes.Duration,
                ColorKeyframes.Duration)));

    public KeyframeList<RandomizableKeyframe<Vector2>> PositionKeyframes { get; } = [];
    public KeyframeList<RandomizableKeyframe<Vector2>> ScaleKeyframes { get; } = [];
    public KeyframeList<RandomizableKeyframe<float>> RotationKeyframes { get; } = [];
    public KeyframeList<Keyframe<BeatmapObjectIndexedColor>> ColorKeyframes { get; } = [];

    public BeatmapObject(string id)
    {
        Id = id;
        
        PositionKeyframes.ListChanged += (_, e) => PositionKeyframesChanged?.Invoke(this, e);
        ScaleKeyframes.ListChanged += (_, e) => ScaleKeyframesChanged?.Invoke(this, e);
        RotationKeyframes.ListChanged += (_, e) => RotationKeyframesChanged?.Invoke(this, e);
        ColorKeyframes.ListChanged += (_, e) => ColorKeyframesChanged?.Invoke(this, e);
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