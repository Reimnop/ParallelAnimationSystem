using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Core.Model;

public class BeatmapData
{
    public event EventHandler<BeatmapObject>? ObjectInserted; 
    public event EventHandler<BeatmapObject>? ObjectRemoved;
    
    public IReadOnlyDictionary<string, BeatmapObject> Objects => objects;

    private readonly Dictionary<string, BeatmapObject> objects = [];

    public bool InsertObject(BeatmapObject obj)
    {
        var id = obj.Id;
        if (!objects.TryAdd(id, obj))
            return false;
        ObjectInserted?.Invoke(this, obj);
        return true;
    }
    
    public bool RemoveObject(string id)
        => RemoveObject(id, out _);
    
    public bool RemoveObject(string id, [MaybeNullWhen(false)] out BeatmapObject obj)
    {
        if (!objects.Remove(id, out obj))
            return false;
        ObjectRemoved?.Invoke(this, obj);
        return true;
    }
}