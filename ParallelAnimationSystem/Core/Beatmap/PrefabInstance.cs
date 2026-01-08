using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ParallelAnimationSystem.Core.Beatmap;

public class PrefabInstance(ObjectId id) : IIndexedObject, INotifyPropertyChanged
{
    public ObjectId Id { get; } = id;

    public string Name
    {
        get => name;
        set => SetField(ref name, value);
    }
    
    private string name = string.Empty;
    
    public event PropertyChangedEventHandler? PropertyChanged;

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