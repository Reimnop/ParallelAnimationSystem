using System.Diagnostics.CodeAnalysis;

namespace ParallelAnimationSystem.Data;

public class ObservableSparseSetEventArgs<T>(int id, T item) : EventArgs
{
    public int Id => id;
    public T Item => item;
}

public class ObservableSparseSet<T> : SparseSet<T>
{
    public event EventHandler<ObservableSparseSetEventArgs<T>>? ItemInserted;
    public event EventHandler<ObservableSparseSetEventArgs<T>>? ItemRemoved;
    
    public override int Insert(T item)
    {
        var id = base.Insert(item);
        ItemInserted?.Invoke(this, new ObservableSparseSetEventArgs<T>(id, item));
        return id;
    }

    public override bool Remove(int id, [MaybeNullWhen(false)] out T item)
    {
        if (!base.Remove(id, out item))
            return false;
        ItemRemoved?.Invoke(this, new ObservableSparseSetEventArgs<T>(id, item));
        return true;
    }
}