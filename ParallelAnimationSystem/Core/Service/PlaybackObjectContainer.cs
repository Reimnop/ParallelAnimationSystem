using System.Diagnostics.CodeAnalysis;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Service;

public class ParentSetEventArgs(int index, int? parentIndex) : EventArgs
{
    public int Index => index;
    public int? ParentIndex => parentIndex;
}

public class PlaybackObjectContainer : IndexedTree<PlaybackObject>
{
    public event EventHandler<IndexedCollectionEntry<PlaybackObject>>? PlaybackObjectInserted;
    public event EventHandler<IndexedCollectionEntry<PlaybackObject>>? PlaybackObjectRemoved;
    public event EventHandler<ParentSetEventArgs>? ParentSet;
    
    public override int Insert(PlaybackObject item)
    {
        var index = base.Insert(item);
        PlaybackObjectInserted?.Invoke(this, new IndexedCollectionEntry<PlaybackObject>(item, index));
        return index;
    }

    public override bool Remove(int index, [MaybeNullWhen(false)] out PlaybackObject item)
    {
        if (!base.Remove(index, out item))
            return false;
        PlaybackObjectRemoved?.Invoke(this, new IndexedCollectionEntry<PlaybackObject>(item, index));
        return true;
    }

    public override bool SetParent(int childIndex, int? parentIndex)
    {
        if (!base.SetParent(childIndex, parentIndex))
            return false;
        ParentSet?.Invoke(this, new ParentSetEventArgs(childIndex, parentIndex));
        return true;
    }
}