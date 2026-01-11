using System.Collections;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core;

public class PerFrameDataCache : IReadOnlyList<PerFrameBeatmapObjectData>
{
    public int Count => count;
    
    private readonly List<PerFrameBeatmapObjectData> cache = [];
    private int count;

    public PerFrameDataCache(int initialCapacity = 1000)
    {
        for (var i = 0; i < initialCapacity; i++)
            cache.Add(new PerFrameBeatmapObjectData());
    }
    
    public void EnsureSize(int size)
    {
        var newCount = Math.Max(size, count);
        while (cache.Count < newCount)
            cache.Add(new PerFrameBeatmapObjectData());
        count = newCount;
    }

    public void Reset()
    {
        count = 0;
    }

    public void Sort(IComparer<PerFrameBeatmapObjectData> comparer)
    {
        cache.Sort(0, count, comparer);
    }
    
    public IEnumerator<PerFrameBeatmapObjectData> GetEnumerator()
    {
        for (var i = 0; i < count; i++)
            yield return cache[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public PerFrameBeatmapObjectData this[int index]
    {
        get
        {
            if (index >= count || index < 0)
                throw new IndexOutOfRangeException();
            return cache[index];
        }
    }
}