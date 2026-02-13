using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using Pamx.Common.Enum;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Rendering.Data;
using ParallelAnimationSystem.Text;

namespace ParallelAnimationSystem.Core.Service;

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
    
    public ShapedRichText? Text 
    {
        get;
        set => SetField(ref field, value);
    }

    public Sequence<Vector2> PositionSequence { get; } = new(Vector2.Lerp, () => Vector2.Zero);
    public Sequence<Vector2> ScaleSequence { get; } = new(Vector2.Lerp , () => Vector2.One);
    public Sequence<float> RotationSequence { get; } = new(float.Lerp, () => 0f);

    public IndirectSequence<BeatmapObjectIndexedColor, BeatmapObjectColor, ThemeColorState> ColorSequence { get; }
        = new(BeatmapObjectColor.Resolve, BeatmapObjectColor.Lerp, 
            _ => new BeatmapObjectColor(new ColorRgb(), new ColorRgb(), 1f));

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