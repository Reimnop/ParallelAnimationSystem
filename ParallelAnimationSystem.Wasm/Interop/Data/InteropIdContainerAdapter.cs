using System.Collections;
using ParallelAnimationSystem.Core.Model;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

public class InteropIdContainerAdapter<T>(IdContainer<T> container): IInteropIdContainerAdapter where T : IStringIdentifiable
{
    public int Count => container.Count;
    
    public object? GetById(string id)
        => container.GetValueOrDefault(id);

    public bool Insert(object? item)
    {
        if (item is not T typedItem)
            return false;
        return container.Insert(typedItem);
    }
    
    public bool Remove(string id)
        => container.Remove(id);

    public IEnumerator GetEnumerator()
    {
        foreach (var (id, item) in container)
            yield return new KeyValuePair<string, object>(id, item);
    }
}