using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Rendering.Data;

namespace ParallelAnimationSystem.Core.Animation;

public class PlaybackObject(Identifier id) : IIdentifiable, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public Identifier Id => id;
    
    public float StartTime
    {
        get;
        set => SetField(ref field, value);
    }
    
    public float EndTime
    {
        get;
        set => SetField(ref field, value);
    }

    public bool IsVisible
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

    public RenderMode RenderMode
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

    public Sequence<Vector2> PositionSequence { get; } = new(Vector2.Lerp);
    public Sequence<Vector2> ScaleSequence { get; } = new(Vector2.Lerp);
    public Sequence<float> RotationSequence { get; } = new(float.Lerp);

    public IndirectSequence<BeatmapObjectIndexedColor, BeatmapObjectColor, ThemeColorStateContext> ColorSequence { get; }
        = new(BeatmapObjectColor.Resolve, BeatmapObjectColor.Lerp);

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