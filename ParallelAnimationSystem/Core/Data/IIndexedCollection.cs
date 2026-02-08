using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Core.Data;

public interface IIndexedCollection<T> : IReadOnlyCollection<IndexedCollectionEntry<T>> where T : IIdentifiable
{
    int GetIndexForId(Identifier id);
    int Insert(T item);
    bool Remove(int index, [MaybeNullWhen(false)] out T item);
    bool Remove(int index);
    bool TryGetItem(int index, [MaybeNullWhen(false)] out T item);
    bool ContainsIndex(int index);
}

