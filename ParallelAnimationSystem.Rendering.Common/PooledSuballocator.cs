using System.Numerics;

namespace ParallelAnimationSystem.Rendering.Common;

public class PooledSuballocator(int capacity = 1024)
{
    public int Capacity => backingAllocator.Capacity;
    
    private readonly BufferSuballocator backingAllocator = new(capacity);
    private readonly Dictionary<int, Stack<Allocation>> pooledAllocationsBySizeClasses = new();
    
    public void Grow(int newCapacity) => backingAllocator.Grow(newCapacity);
    
    public Allocation Allocate(int size, GrowBufferCallback? growCallback = null)
    {
        Allocation alloc;
        var grown = false;
        var oldCapacity = Capacity;
        while (!TryAllocate(size, out alloc))
        {
            Grow(Capacity * 2);
            grown = true;
        }
        
        if (grown && growCallback != null)
            growCallback(oldCapacity, Capacity);
        
        return alloc;
    }
    
    public bool TryAllocate(int size, out Allocation allocation)
    {
        var sizeClass = GetSizeClass(size);
        
        if (pooledAllocationsBySizeClasses.TryGetValue(sizeClass, out var stack) && stack.Count > 0)
        {
            allocation = stack.Pop();
            return true;
        }

        if (backingAllocator.TryAllocate(sizeClass, out allocation))
            return true;

        allocation = default;
        return false;
    }
    
    public void Free(Allocation alloc)
    {
        // we use alloc.Size directly because it's guaranteed to be a valid size class
        if (!pooledAllocationsBySizeClasses.TryGetValue(alloc.Size, out var stack))
        {
            stack = new Stack<Allocation>();
            pooledAllocationsBySizeClasses[alloc.Size] = stack;
        }

        stack.Push(alloc);
    }

    // inspired by jemalloc
    private int GetSizeClass(int size)
    {
        // minimum size class
        if (size <= 16)
            return 16;

        var exp = BitOperations.Log2((uint)(size - 1)) ; // exponent of the range size falls into
        var pow2 = 1 << (exp + 1);                       // top of the range, e.g. 32, 64, 128...
        var step = pow2 >> 2;                            // quarter-step within the range, e.g. 8, 16, 32...

        // round up to the next multiple of `step` at or above `size`
        return (size + step - 1) / step * step;
    }
}
