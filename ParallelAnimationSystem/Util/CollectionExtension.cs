namespace ParallelAnimationSystem.Util;

public static class CollectionExtension
{
    public static TValue GetOrInsert<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory) where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;
        
        value = factory();
        dictionary.Add(key, value);

        return value;
    }
    
    public static int BinarySearchKey<T, TKey>(this IReadOnlyList<T> list, TKey key, Func<T, TKey> keySelector, IComparer<TKey> comparer)
    {
        var min = 0;
        var max = list.Count - 1;
        while (min <= max)
        {
            var mid = (min + max) / 2;
            var midKey = keySelector(list[mid]);
            var comparison = comparer.Compare(midKey, key);
            if (comparison == 0)
                return mid;
            if (comparison < 0)
                min = mid + 1;
            else
                max = mid - 1;
        }

        return ~min;
    }

    public static void EnsureCount<T>(this List<T?> list, int count)
    {
        while (list.Count < count)
            list.Add(default);
    }
}