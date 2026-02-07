namespace ParallelAnimationSystem.Core.Data;

public class KeyframeList<T>() : SortedList<T>(comparer)
    where T : struct, IKeyframe
{
    private static readonly KeyframeComparer<T> comparer = new();
    
    public event EventHandler<KeyframeList<T>>? ListChanged;
    
    public float Duration => Count == 0 ? 0f : this[^1].Time;

    public override void Add(T item)
    {
        base.Add(item);
        ListChanged?.Invoke(this, this);
    }

    public override bool Remove(T item)
    {
        if (!base.Remove(item))
            return false;
        
        ListChanged?.Invoke(this, this);
        return true;
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