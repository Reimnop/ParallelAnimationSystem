using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ParallelAnimationSystem.Core.Beatmap;

public class Prefab(ObjectId id) : IIndexedObject, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public ObjectId Id { get; } = id;

    public string Name
    {
        get => name;
        set => SetField(ref name, value);
    }

    public float Offset
    {
        get => offset;
        set => SetField(ref offset, value);
    }
    
    public BeatmapObjectsContainer BeatmapObjects { get; } = new();
    
    private string name = string.Empty;
    private float offset = 0f;

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
    
    public float CalculateKillTime(float instanceStartTime)
    {
        var killTime = float.NegativeInfinity;
        
        foreach (var obj in BeatmapObjects)
        {
            var objectKillTime = obj.CalculateKillTime(instanceStartTime);
            killTime = MathF.Max(killTime, objectKillTime);
        }

        return killTime;
    }
}