using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Util;

public interface IResettable
{
    void Reset();
}

public class MemoryPool<T> where T : IResettable
{
    public int Capacity { get; }
    public int FreeCount => pool.Count;
    public int RentedCount => Capacity - FreeCount;

    private readonly ConcurrentStack<T> pool = new();

    public MemoryPool(int capacity, Func<T> factory)
    {
        Capacity = capacity;
        
        for (var i = 0; i < capacity; i++)
        {
            var item = factory();
            item.Reset(); // ensure item is in a clean state before being added to the pool
            pool.Push(item);
        }
    }
    
    public bool TryRent([MaybeNullWhen(false)] out T item)
        => pool.TryPop(out item);
    
    public void Return(T item)
    {
        item.Reset();
        pool.Push(item);
    }
}