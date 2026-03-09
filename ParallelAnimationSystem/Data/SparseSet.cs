using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Data;

public class SparseSet<T> : IReadOnlyCollection<KeyValuePair<int, T>>
{
    private readonly List<int> idToIndex = [];
    private readonly List<int> indexToId = [];
    private readonly List<T> items = [];
    
    public int Count => items.Count;
    
    public T this[int id] => !TryGet(id, out var item) 
        ? throw new KeyNotFoundException($"ID '{id}' not found") 
        : item;

    public virtual int Insert(T item)
    {
        items.Add(item);
        
        var index = items.Count - 1;
        if (index < indexToId.Count)
            return indexToId[index];
        
        indexToId.Add(index);
        idToIndex.Add(index);
        return index;
    }
    
    public virtual bool Remove(int id, [MaybeNullWhen(false)] out T item)
    {
        if (!TryGetIndex(id, out var index))
        {
            item = default;
            return false;
        }
        
        item = items[index];
        
        var lastIndex = items.Count - 1;
        if (index != lastIndex)
        {
            // swap with last item
            (items[index], items[lastIndex]) = (items[lastIndex], items[index]);
            
            // keep indexToId synced
            (indexToId[index], indexToId[lastIndex]) = (indexToId[lastIndex], indexToId[index]);
            
            // update idToIndex for the swapped item
            idToIndex[indexToId[index]] = index;
            idToIndex[indexToId[lastIndex]] = lastIndex;
        }
        
        // remove last item
        items.RemoveAt(lastIndex);
        return true;
    }
    
    public bool TryGet(int id, [MaybeNullWhen(false)] out T item)
    {
        if (TryGetIndex(id, out var index))
        {
            item = items[index];
            return true;
        }
        item = default;
        return false;
    }
    
    public bool Contains(int id)
    {
        if (id < 0 || id >= idToIndex.Count)
            return false;
        var index = idToIndex[id];
        return index < items.Count;
    }

    private bool TryGetIndex(int id, out int index)
    {
        if (id < 0 || id >= idToIndex.Count)
        {
            index = -1;
            return false;
        }
        index = idToIndex[id];
        return index < items.Count;
    }

    public IEnumerator<KeyValuePair<int, T>> GetEnumerator()
    {
        for (var i = 0; i < items.Count; i++)
        {
            var id = idToIndex[i];
            yield return new KeyValuePair<int, T>(id, items[i]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}