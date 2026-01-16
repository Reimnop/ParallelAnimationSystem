namespace ParallelAnimationSystem.Util;

public static class ListExtension
{
    public static int BinarySearchKey<T, TKey>(this List<T> list, TKey key, Func<T, TKey> keySelector, IComparer<TKey> comparer)
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