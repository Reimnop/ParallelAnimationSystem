using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Core.Data;

public class IndexedTree<T> : IReadOnlyCollection<T> where T : IIdentifiable
{
    public int Count { get; private set; }

    private readonly List<T?> items = [];
    private readonly List<bool> exists = [];
    private readonly List<int> parentByIndex = [];
    private readonly List<List<int>> childrenByIndex = [];
    
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
        parentByIndex.Add(-1);
        childrenByIndex.Add([]);
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
    
    public bool TryGetParentIndex(int childIndex, out int parentIndex)
    {
        parentIndex = -1;
        if (childIndex < 0 || childIndex >= items.Count)
            return false;
        var parent = parentByIndex[childIndex];
        if (parent < 0)
            return false;
        parentIndex = parent;
        return true;
    }
    
    public bool TryGetChildrenIndices(int parentIndex, [MaybeNullWhen(false)] out IReadOnlyList<int> childrenIndices)
    {
        childrenIndices = null;
        if (parentIndex < 0 || parentIndex >= items.Count)
            return false;
        childrenIndices = childrenByIndex[parentIndex];
        return true;
    }

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

    public bool SetParent(int childIndex, int? parentIndex)
    {
        if (childIndex < 0 || childIndex >= items.Count) 
            return false;
        
        // detach parent from child
        var oldChildParent = parentByIndex[childIndex];
        if (oldChildParent >= 0)
        {
            childrenByIndex[oldChildParent].Remove(childIndex);
            parentByIndex[childIndex] = -1;
        }
        
        // if we're setting it to null, we're done here
        if (!parentIndex.HasValue)
            return true;
        
        var parentIndexValue = parentIndex.Value;
        
        // check if indices are valid
        if (parentIndexValue < 0 || parentIndexValue >= items.Count)
            return false;
        
        // set new parent
        parentByIndex[childIndex] = parentIndexValue;
        
        // add child to parent's children list
        childrenByIndex[parentIndexValue].Add(childIndex);
        return true;
    }
    
    public bool ContainsIndex(int index)
    {
        if (index < 0 || index >= items.Count)
            return false;
        return exists[index];
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (!exists[i])
                continue;
            var item = items[i];
            Debug.Assert(item is not null);
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}