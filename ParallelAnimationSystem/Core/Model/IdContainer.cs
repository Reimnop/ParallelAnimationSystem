using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Core.Model;

public class IdContainer<T> : Dictionary<string, T> where T : IStringIdentifiable
{
    public event EventHandler<T>? Inserted;
    public event EventHandler<T>? Removed;

    public bool Insert(T obj)
    {
        var id = obj.Id;
        if (!TryAdd(id, obj))
            return false;
        Inserted?.Invoke(this, obj);
        return true;
    }
    
    public new bool Remove(string id)
        => Remove(id, out _);
    
    public new bool Remove(string id, [MaybeNullWhen(false)] out T obj)
    {
        if (!base.Remove(id, out obj))
            return false;
        Removed?.Invoke(this, obj);
        return true;
    }
}