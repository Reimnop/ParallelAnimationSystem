using System.Collections;
using ParallelAnimationSystem.Core.Model;

namespace ParallelAnimationSystem.Wasm.Interop.Data;

public interface IIdContainerInteropWrapper : IEnumerable<KeyValuePair<string, IStringIdentifiable>>
{
    int Count { get; }
    
    IStringIdentifiable? GetById(string id);
    bool Insert(IStringIdentifiable item);
    bool Remove(string id);
}

public class IdContainerInteropWrapper<T>(IdContainer<T> container): IIdContainerInteropWrapper where T : IStringIdentifiable
{
    public int Count => container.Count;
    
    public IStringIdentifiable? GetById(string id)
        => container.GetValueOrDefault(id);

    public bool Insert(IStringIdentifiable item)
    {
        return container.Insert((T)item);
    }
    
    public bool Remove(string id)
        => container.Remove(id);

    public IEnumerator<KeyValuePair<string, IStringIdentifiable>> GetEnumerator()
    {
        foreach (var (id, item) in container)
            yield return new KeyValuePair<string, IStringIdentifiable>(id, item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}