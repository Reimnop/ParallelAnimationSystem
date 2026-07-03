namespace ParallelAnimationSystem.Rendering.Common;

public delegate void GrowBufferCallback(int oldCapacity, int newCapacity);

public class BufferSuballocator
{
    public int Capacity { get; private set; }

    private static readonly IComparer<Allocation> OffsetComparer =
        Comparer<Allocation>.Create((a, b) => a.Offset.CompareTo(b.Offset));
    
    private readonly List<Allocation> freeRanges = [];

    public BufferSuballocator(int capacity = 1024)
    {
        Capacity = capacity;
        freeRanges.Add(new Allocation(Offset: 0, Size: capacity));
    }

    public void Grow(int capacity)
    {
        if (capacity <= Capacity)
            throw new InvalidOperationException($"Can not grow buffer allocator from size {this.Capacity} to size {capacity}");
        
        if (freeRanges.Count == 0)
        {
            // buffer was completely full, new space is the only free range
            freeRanges.Add(new Allocation(Offset: Capacity, Size: capacity - Capacity));
            Capacity = capacity;
            return;
        }

        var lastFree = freeRanges[^1];
        var lastFreeEnd = lastFree.Offset + lastFree.Size;

        // grow the last free range
        if (lastFreeEnd == Capacity)
        {
            var newLastFreeSize = capacity - Capacity + lastFree.Size;
            lastFree = lastFree with { Size = newLastFreeSize };
            freeRanges[^1] = lastFree;
        }
        // append a new free range
        else
        {
            var newLastFreeSize = capacity - Capacity;
            lastFree = new Allocation(Offset: Capacity, Size: newLastFreeSize);
            freeRanges.Add(lastFree);
        }

        // set new capacity
        Capacity = capacity;
    }

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

    public bool TryAllocate(int size, out Allocation alloc)
    {
        // find a free range with a suitable size
        for (var i = 0; i < freeRanges.Count; i++)
        {
            var range = freeRanges[i];
            if (range.Size >= size)
            {
                alloc = range with { Size = size };
                freeRanges.RemoveAt(i);

                var usedEnd = alloc.Offset + size;
                var rangeEnd = range.Offset + range.Size;
                
                if (usedEnd < rangeEnd)
                    freeRanges.Insert(i, new Allocation(Offset: usedEnd, Size: rangeEnd - usedEnd));

                return true;
            }
        }
        
        // OOM
        alloc = default;
        return false;
    }

    public void Free(Allocation alloc)
    {
        var idx = freeRanges.BinarySearch(alloc, OffsetComparer);
        if (idx < 0) idx = ~idx;
        freeRanges.Insert(idx, alloc);
        MergeAdjacent(idx);
    }

    private void MergeAdjacent(int idx)
    {
        if (idx + 1 < freeRanges.Count && freeRanges[idx].Offset + freeRanges[idx].Size == freeRanges[idx + 1].Offset)
        {
            var merged = new Allocation(
                Offset: freeRanges[idx].Offset,
                Size: freeRanges[idx].Size + freeRanges[idx + 1].Size);
            freeRanges[idx] = merged;
            freeRanges.RemoveAt(idx + 1);
        }
        
        if (idx > 0 && freeRanges[idx - 1].Offset + freeRanges[idx - 1].Size == freeRanges[idx].Offset)
        {
            var merged = new Allocation(
                Offset: freeRanges[idx - 1].Offset,
                Size: freeRanges[idx - 1].Size + freeRanges[idx].Size);
            freeRanges[idx - 1] = merged;
            freeRanges.RemoveAt(idx);
        }
    }
}
