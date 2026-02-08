using System.Diagnostics.CodeAnalysis;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Animation;

public class PlaybackObjectContainer : IndexedTree<PlaybackObject>
{
    public event EventHandler<IndexedCollectionEntry<PlaybackObject>>? PlaybackObjectInserted;
    public event EventHandler<IndexedCollectionEntry<PlaybackObject>>? PlaybackObjectRemoved;
    
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
}