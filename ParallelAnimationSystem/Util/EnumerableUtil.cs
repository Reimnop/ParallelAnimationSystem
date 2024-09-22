namespace ParallelAnimationSystem.Util;

public static class EnumerableUtil
{
    public static IEnumerable<(int Index, T Value)> Indexed<T>(this IEnumerable<T> enumerable)
    {
        var i = 0;
        foreach (var value in enumerable)
            yield return (i++, value);
    }
}