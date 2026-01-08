using System.Collections;

namespace ParallelAnimationSystem.Util;

public static class CollectionUtil
{
    private class CollectionAdapter<TIn, TOut>(IReadOnlyCollection<TIn> inner, Func<TIn, TOut> func) : IReadOnlyCollection<TOut>
    {
        public int Count => inner.Count;
        
        public IEnumerator<TOut> GetEnumerator()
        {
            foreach (var item in inner)
                yield return func(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    
    public static IReadOnlyCollection<TOut> AdaptType<TIn, TOut>(this IReadOnlyCollection<TIn> input, Func<TIn, TOut> func)
        => new CollectionAdapter<TIn, TOut>(input, func);
}