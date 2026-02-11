using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ParallelAnimationSystem.Core.Model;

public class BeatmapPrefab(string id) : IStringIdentifiable, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public string Id => id;

    public string Name
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public float Offset
    {
        get;
        set => SetField(ref field, value);
    }
    
    public IdContainer<BeatmapObject> Objects { get; } = new();

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