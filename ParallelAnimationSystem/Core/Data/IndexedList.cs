using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Core.Data;

public class IndexedList<T> : IIndexedCollection<T> where T : IIdentifiable
{
    public int Count { get; private set; }

    private readonly List<T?> items = [];
    private readonly List<bool> exists = [];
    
    private readonly Dictionary<Identifier, int> indexById = [];

    public int GetIndexForId(Identifier id)
    {
        // if object id is known, return the existing index
        if (indexById.TryGetValue(id, out var index))
            return index;
        
        // otherwise, add a new item with default value
        index = items.Count;
        items.Add(default);
        exists.Add(false);
        indexById[id] = index;
        return index;
    }

    public virtual int Insert(T item)
    {
        var id = item.Id;

        // get the index for the item's id
        var index = GetIndexForId(id);
        if (exists[index])
            throw new InvalidOperationException($"Item with ID '{id}' already exists");
        
        exists[index] = true;
        items[index] = item;
        Count++;
        return index;
    }

    public virtual bool Remove(int index, [MaybeNullWhen(false)] out T item)
    {
        item = default;
        
        if (index < 0 || index >= items.Count)
            return false;
        
        if (!exists[index])
            return false;
        
        item = items[index];
        Debug.Assert(item is not null);
        
        exists[index] = false;
        items[index] = default;
        Count--;
        return true;
    }
    
    public bool Remove(int index)
        => Remove(index, out _);

    public bool TryGetItem(int index, [MaybeNullWhen(false)] out T item)
    {
        item = default;
        if (index < 0 || index >= items.Count)
            return false;
        if (!exists[index])
            return false;
        item = items[index];
        Debug.Assert(item is not null);
        return true;
    }
    
    public bool ContainsIndex(int index)
    {
        if (index < 0 || index >= items.Count)
            return false;
        return exists[index];
    }

    public IEnumerator<IndexedCollectionEntry<T>> GetEnumerator()
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (!exists[i])
                continue;
            var item = items[i];
            Debug.Assert(item is not null);
            yield return new IndexedCollectionEntry<T>(item, i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

