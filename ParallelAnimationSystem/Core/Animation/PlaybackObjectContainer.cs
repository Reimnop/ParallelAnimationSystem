using System.Diagnostics.CodeAnalysis;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Animation;

public class PlaybackObjectContainer : IndexedTree<PlaybackObject>
{
    public event EventHandler<IndexedTreeEntry<PlaybackObject>>? PlaybackObjectInserted;
    public event EventHandler<IndexedTreeEntry<PlaybackObject>>? PlaybackObjectRemoved;
    
    public override int Insert(PlaybackObject item)
    {
        var index = base.Insert(item);
        PlaybackObjectInserted?.Invoke(this, new IndexedTreeEntry<PlaybackObject>(item, index));
        return index;
    }

    public override bool Remove(int index, [MaybeNullWhen(false)] out PlaybackObject item)
    {
        if (!base.Remove(index, out item))
            return false;
        PlaybackObjectRemoved?.Invoke(this, new IndexedTreeEntry<PlaybackObject>(item, index));
        return true;
    }
}