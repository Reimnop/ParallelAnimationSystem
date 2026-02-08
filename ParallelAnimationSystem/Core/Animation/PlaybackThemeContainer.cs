using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Animation;

public class PlaybackThemeContainer : IndexedList<PlaybackTheme>
{
    public event EventHandler<IndexedCollectionEntry<PlaybackTheme>>? PlaybackThemeInserted;
    public event EventHandler<IndexedCollectionEntry<PlaybackTheme>>? PlaybackThemeRemoved;

    public override int Insert(PlaybackTheme item)
    {
        var index = base.Insert(item);
        PlaybackThemeInserted?.Invoke(this, new IndexedCollectionEntry<PlaybackTheme>(item, index));
        return index;
    }
    
    public override bool Remove(int index, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out PlaybackTheme item)
    {
        if (!base.Remove(index, out item))
            return false;
        PlaybackThemeRemoved?.Invoke(this, new IndexedCollectionEntry<PlaybackTheme>(item, index));
        return true;
    }
}