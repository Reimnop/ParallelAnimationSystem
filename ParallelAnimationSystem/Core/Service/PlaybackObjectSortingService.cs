using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Service;

public class PlaybackObjectSortingService : IDisposable
{
    private struct ObjectSortItem
    {
        public RenderLayer RenderLayer;
        public float RenderDepth;
        public int ParentDepth;
        public float StartTime;
        public Identifier Id;
        public int Index;
    }
    
    private bool sortRankDirty = false;
    private uint[] sortRanks = new uint[1000];
    
    private readonly HashSet<PlaybackObject> visibleObjects = [];
    
    private readonly PlaybackObjectContainer playbackObjects;
    private readonly ILogger<PlaybackObjectSortingService> logger;
    
    public PlaybackObjectSortingService(PlaybackObjectContainer playbackObjects, ILogger<PlaybackObjectSortingService> logger)
    {
        this.playbackObjects = playbackObjects;
        this.logger = logger;
        
        // add all visible objects to the set
        foreach (var (obj, _) in this.playbackObjects)
        {
            if (obj.Type == PlaybackObjectType.Visible)
                InsertVisibleObject(obj);
            
            obj.PropertyChanged += OnPlaybackObjectPropertyChanged;
        }
        
        // subscribe to changes in the playback objects
        this.playbackObjects.PlaybackObjectInserted += OnPlaybackObjectInserted;
        this.playbackObjects.PlaybackObjectRemoved += OnPlaybackObjectRemoved;
        this.playbackObjects.ParentSet += OnParentSet;
    }
    
    public void Dispose()
    {
        foreach (var (obj, _) in playbackObjects)
            obj.PropertyChanged -= OnPlaybackObjectPropertyChanged;
        
        // unsubscribe from events
        playbackObjects.PlaybackObjectInserted -= OnPlaybackObjectInserted;
        playbackObjects.PlaybackObjectRemoved -= OnPlaybackObjectRemoved;
        playbackObjects.ParentSet -= OnParentSet;
    }

    private void OnPlaybackObjectInserted(object? sender, IndexedCollectionEntry<PlaybackObject> e)
    {
        if (e.Item.Type == PlaybackObjectType.Visible)
            InsertVisibleObject(e.Item);
        
        e.Item.PropertyChanged += OnPlaybackObjectPropertyChanged;
    }

    private void OnPlaybackObjectRemoved(object? sender, IndexedCollectionEntry<PlaybackObject> e)
    {
        if (e.Item.Type == PlaybackObjectType.Visible)
            RemoveVisibleObject(e.Item);
        
        e.Item.PropertyChanged -= OnPlaybackObjectPropertyChanged;
    }

    private void InsertVisibleObject(PlaybackObject obj)
    {
        visibleObjects.Add(obj);
        sortRankDirty = true;
    }
    
    private void RemoveVisibleObject(PlaybackObject obj)
    {
        visibleObjects.Remove(obj);
        sortRankDirty = true;
    }
    
    private void OnPlaybackObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not PlaybackObject obj)
            return;
        
        switch (e.PropertyName)
        {
            case nameof(PlaybackObject.Type):
                if (obj.Type == PlaybackObjectType.Visible)
                    InsertVisibleObject(obj);
                else
                    RemoveVisibleObject(obj);
                break;
            case nameof(PlaybackObject.RenderDepth):
            case nameof(PlaybackObject.StartTime):
                sortRankDirty = true;
                break;
        }
    }
    
    private void OnParentSet(object? sender, ParentSetEventArgs e)
    {
        sortRankDirty = true;
    }

    public uint[] GetObjectIndexToSortRankMapping()
    {
        if (sortRankDirty)
        {
            sortRankDirty = false;
            ComputeSortRank();
        }
        
        return sortRanks;
    }

    private void ComputeSortRank()
    {
        logger.LogInformation("Computing visible object sort rank");
        
        var sortItems = GetSortData(visibleObjects);
        var ranks = new uint[sortItems.Length];
        FillWithIncrementing(ranks);
        
        // sort ranks
        Array.Sort(ranks, (i, j) =>
        {
            ref var x = ref sortItems[i];
            ref var y = ref sortItems[j];
            
            var layerComparison = x.RenderLayer.CompareTo(y.RenderLayer);
            if (layerComparison != 0)
                return layerComparison;
            
            var renderDepthComparison = y.RenderDepth.CompareTo(x.RenderDepth);
            if (renderDepthComparison != 0)
                return renderDepthComparison;
            
            var parentDepthComparison = y.ParentDepth.CompareTo(x.ParentDepth);
            if (parentDepthComparison != 0)
                return parentDepthComparison;
            
            var startTimeComparison = x.StartTime.CompareTo(y.StartTime);
            if (startTimeComparison != 0)
                return startTimeComparison;
            
            return x.Id.Value.CompareTo(y.Id.Value);
        });
        
        // make sure sortRanks has enough capacity
        var maxIndex = sortItems.Max(d => d.Index);
        if (maxIndex >= sortRanks.Length)
        {
            var newSize = Math.Max(maxIndex + 1, sortRanks.Length * 2);
            Array.Resize(ref sortRanks, newSize);
        }
        
        // map from object index to sort rank
        for (uint sortedPosition = 0; sortedPosition < ranks.Length; sortedPosition++)
        {
            var rank = ranks[sortedPosition];
            ref var sortItem = ref sortItems[rank];
            sortRanks[sortItem.Index] = sortedPosition;
        }
    }

    private ObjectSortItem[] GetSortData(IReadOnlyCollection<PlaybackObject> objects)
    {
        var sortData = new ObjectSortItem[objects.Count];
        var i = 0;
        foreach (var obj in objects)
        {
            var objIndex = playbackObjects.GetIndexForId(obj.Id);
            TraverseParents(objIndex, out var parentDepth, out var renderLayer);
            
            sortData[i++] = new ObjectSortItem
            {
                RenderLayer = renderLayer,
                RenderDepth = obj.RenderDepth,
                ParentDepth = parentDepth,
                StartTime = obj.StartTime,
                Id = obj.Id,
                Index = objIndex
            };
        }
        return sortData;
    }

    private void FillWithIncrementing(uint[] indices)
    {
        for (uint i = 0; i < indices.Length; i++)
            indices[i] = i;
    }

    private void TraverseParents(int playbackObjectIndex, out int parentDepth, out RenderLayer renderLayer)
    {
        parentDepth = 0;
        renderLayer = RenderLayer.Foreground;
        
        if (!playbackObjects.TryGetItem(playbackObjectIndex, out var playbackObject))
            return;

        while (true)
        {
            parentDepth++;
            
            if (playbackObject.Type == PlaybackObjectType.Camera)
                renderLayer = RenderLayer.Camera;

            if (!playbackObjects.TryGetParentIndex(playbackObjectIndex, out playbackObjectIndex)) 
                break;
            
            if (!playbackObjects.TryGetItem(playbackObjectIndex, out playbackObject))
                break;
        }
    }
}