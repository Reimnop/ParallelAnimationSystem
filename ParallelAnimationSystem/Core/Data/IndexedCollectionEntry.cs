namespace ParallelAnimationSystem.Core.Data;

public struct IndexedCollectionEntry<T>(T item, int index) where T : IIdentifiable
{
    public T Item => item;
    public int Index => index;
    
    public void Deconstruct(out T item, out int index)
    {
        item = Item;
        index = Index;
    }
}

