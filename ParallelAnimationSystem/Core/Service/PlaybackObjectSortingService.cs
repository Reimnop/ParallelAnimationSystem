using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Service;

public class PlaybackObjectSortingService : IDisposable
{
    private struct ObjectSortData
    {
        public RenderLayer RenderLayer;
        public float RenderDepth;
        public int ParentDepth;
        public float StartTime;
        public Identifier Id;
    }
    
    private bool sortOrderDirty = false;
    private uint[] sortOrderIndices = new uint[1000];
    
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
        sortOrderDirty = true;
    }
    
    private void RemoveVisibleObject(PlaybackObject obj)
    {
        visibleObjects.Remove(obj);
        sortOrderDirty = true;
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
                sortOrderDirty = true;
                break;
        }
    }
    
    private void OnParentSet(object? sender, ParentSetEventArgs e)
    {
        sortOrderDirty = true;
    }

    public uint[] GetSortOrderIndices()
    {
        if (sortOrderDirty)
        {
            sortOrderDirty = false;
            ComputeSortOrderIndices();
        }
        
        return sortOrderIndices;
    }

    private void ComputeSortOrderIndices()
    {
        logger.LogInformation("Computing object sort order");
        
        var sortDataStructs = GetSortData(visibleObjects);
        var indices = new uint[sortDataStructs.Length];
        FillWithIncrementingIndices(indices);
        
        // sort indices
        Array.Sort(indices, (i, j) =>
        {
            ref var x = ref sortDataStructs[i];
            ref var y = ref sortDataStructs[j];
            
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
        
        // convert sorted indices to object indices
        for (uint sortedPosition = 0; sortedPosition < indices.Length; sortedPosition++)
        {
            var originalIndex = indices[sortedPosition];
            ref var sortData = ref sortDataStructs[originalIndex];
            
            var objectIndex = playbackObjects.GetIndexForId(sortData.Id);
            if (objectIndex >= sortOrderIndices.Length)
                Array.Resize(ref sortOrderIndices, Math.Max(objectIndex + 1, sortOrderIndices.Length * 2));
            
            sortOrderIndices[objectIndex] = sortedPosition;
        }
    }

    private ObjectSortData[] GetSortData(IReadOnlyCollection<PlaybackObject> objects)
    {
        var sortData = new ObjectSortData[objects.Count];
        var i = 0;
        foreach (var obj in objects)
        {
            var objIndex = playbackObjects.GetIndexForId(obj.Id);
            TraverseParents(objIndex, out var parentDepth, out var renderLayer);
            
            sortData[i++] = new ObjectSortData
            {
                RenderLayer = renderLayer,
                RenderDepth = obj.RenderDepth,
                ParentDepth = parentDepth,
                StartTime = obj.StartTime,
                Id = obj.Id
            };
        }
        return sortData;
    }

    private void FillWithIncrementingIndices(uint[] indices)
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