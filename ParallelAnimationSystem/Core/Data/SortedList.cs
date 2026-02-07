using System.Collections;

namespace ParallelAnimationSystem.Core.Data;

public class SortedList<T>(IComparer<T> comparer) : IReadOnlyList<T>
{
    public int Count => items.Count;

    private readonly List<T> items = [];

    public T this[int index] => items[index];
    
    public virtual void Add(T item)
    {
        var index = items.BinarySearch(item, comparer);
        if (index < 0)
            index = ~index;
        items.Insert(index, item);
    }
    
    public virtual bool Remove(T item)
    {
        var index = items.BinarySearch(item, comparer);
        if (index < 0)
            return false;
        items.RemoveAt(index);
        return true;
    }
    
    public virtual void RemoveAt(int index)
        => items.RemoveAt(index);
    
    public virtual void Clear()
        => items.Clear();

    public virtual void Replace(IEnumerable<T> collection)
    {
        items.Clear();
        items.AddRange(collection);
        items.Sort(comparer);
    }
    
    public int BinarySearch(T item)
    {
        return items.BinarySearch(item, comparer);
    }
    
    public int IndexOf(T item)
    {
        var index = items.BinarySearch(item, comparer);
        return index < 0 ? -1 : index;
    }

    public bool Contains(T item)
    {
        var index = items.BinarySearch(item, comparer);
        return index >= 0;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}