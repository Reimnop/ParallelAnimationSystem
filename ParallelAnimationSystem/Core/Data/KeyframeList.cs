namespace ParallelAnimationSystem.Core.Data;

public class KeyframeList<T>() : SortedList<T>(Comparer)
    where T : IKeyframe
{
    private static readonly KeyframeComparer<T> Comparer = new();
    
    public event EventHandler<KeyframeList<T>>? ListChanged;
    
    public float Duration => Count == 0 ? 0f : this[^1].Time;

    public override void Add(T item)
    {
        base.Add(item);
        ListChanged?.Invoke(this, this);
    }
    
    public override void RemoveAt(int index)
    {
        base.RemoveAt(index);
        ListChanged?.Invoke(this, this);
    }
    
    public override void Clear()
    {
        base.Clear();
        ListChanged?.Invoke(this, this);
    }
    
    public override void Replace(IEnumerable<T> collection)
    {
        base.Replace(collection);
        ListChanged?.Invoke(this, this);
    }
}