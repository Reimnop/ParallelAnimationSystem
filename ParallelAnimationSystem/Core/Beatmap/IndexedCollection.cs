using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Core.Beatmap;

public delegate T IndexedItemFactory<out T>(int numericId) where T : class, IIndexedObject;

public class IndexedCollection<T> : IReadOnlyCollection<T> where T : class, IIndexedObject
{
    public event EventHandler<T>? ItemAdded;
    public event EventHandler<T>? ItemRemoved;
    
    public int Count => occupiedNumericIds.Count;
    
    private readonly List<T?> list = [];
    private readonly Dictionary<string, int> stringIdToNumericId = [];
    private readonly List<int> occupiedNumericIds = []; // This list is always sorted

    public T Add(IndexedItemFactory<T> factory)
    {
        var numericId = list.Count;
        var item = factory(numericId);
        
        if (stringIdToNumericId.ContainsKey(item.Id.String))
            throw new ArgumentException($"An item with the ID '{item.Id.String}' already exists in the collection.");
        
        list.Add(item);
        stringIdToNumericId[item.Id.String] = numericId;
        occupiedNumericIds.Add(numericId);
        
        ItemAdded?.Invoke(this, item);
        return item;
    }

    public bool Remove(string id)
    {
        if (!stringIdToNumericId.TryGetValue(id, out var numericId))
            return false;
        
        return Remove(numericId);
    }

    public bool Remove(int id)
    {
        var item = list[id];
        if (item is null)
            return false;
        
        list[id] = null;
        stringIdToNumericId.Remove(item.Id.String);
        
        var index = occupiedNumericIds.BinarySearch(id);
        Debug.Assert(index >= 0);
        occupiedNumericIds.RemoveAt(index);
        ItemRemoved?.Invoke(this, item);
        
        return true;
    }
    
    public bool TryConvertStringIdToNumericId(string id, out int numericId)
        => stringIdToNumericId.TryGetValue(id, out numericId);
    
    public bool TryGet(string id, [MaybeNullWhen(false)] out T item)
    {
        item = null;
        if (!stringIdToNumericId.TryGetValue(id, out var numericId))
            return false;
        
        return TryGet(numericId, out item);
    }

    public bool TryGet(int id, [MaybeNullWhen(false)] out T item)
    {
        item = null;
        if (id < 0 || id >= list.Count)
            return false;
        
        item = list[id];
        return item is not null;
    }
    
    public bool Contains(string id) 
        => stringIdToNumericId.ContainsKey(id);
    
    public bool Contains(int id)
    {
        if (id < 0 || id >= list.Count)
            return false;
        
        return list[id] is not null;
    }

    public T this[int id]
    {
        get
        {
            if (!TryGet(id, out var item))
                throw new KeyNotFoundException($"No item with ID '{id}' found in the collection.");
            return item;
        }
    }
    
    public T this[string id]
    {
        get
        {
            if (!TryGet(id, out var item))
                throw new KeyNotFoundException($"No item with ID '{id}' found in the collection.");
            return item;
        }
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var numericId in occupiedNumericIds)
        {
            var item = list[numericId];
            Debug.Assert(item is not null);
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}