using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ParallelAnimationSystem.Core.Model;

public class BeatmapPrefabInstance(string id) : IStringIdentifiable, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public string Id => id;

    public float StartTime
    {
        get;
        set => SetField(ref field, value);
    }
    
    public Vector2 Position
    {
        get;
        set => SetField(ref field, value);
    }

    public Vector2 Scale
    {
        get;
        set => SetField(ref field, value);
    }
    
    public float Rotation
    {
        get;
        set => SetField(ref field, value);
    }
    
    public string? PrefabId
    {
        get;
        set => SetField(ref field, value);
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