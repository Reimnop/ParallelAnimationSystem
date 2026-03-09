using System.ComponentModel;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Shape;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.Handle;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Service;

public class MeshCacheService
{
    private class CacheItem(Identifier id, MeshHandle meshHandle) : IIdentifiable
    {
        public Identifier Id => id;
        public MeshHandle MeshHandle => meshHandle;
        public int RefCount { get; set; }
    }
    
    private readonly IRenderQueue renderQueue;
    private readonly PlaybackObjectContainer playbackObjects;

    private readonly IndexedList<CacheItem> cacheItems = [];
    private readonly List<int?> objectIndexToCacheIndex = [];

    public MeshCacheService(IRenderQueue renderQueue, PlaybackObjectContainer playbackObjects)
    {
        this.renderQueue = renderQueue;
        this.playbackObjects = playbackObjects;

        foreach (var entry in playbackObjects)
        {
            if (entry.Item.Type == PlaybackObjectType.Visible)
                InsertVisiblePlaybackObject(entry);
        }
        
        this.playbackObjects.PlaybackObjectInserted += OnPlaybackObjectInserted;
        this.playbackObjects.PlaybackObjectRemoved += OnPlaybackObjectRemoved;
    }
    
    public void Dispose()
    {
        playbackObjects.PlaybackObjectInserted -= OnPlaybackObjectInserted;
        playbackObjects.PlaybackObjectRemoved -= OnPlaybackObjectRemoved;

        foreach (var entry in playbackObjects)
        {
            if (entry.Item.Type == PlaybackObjectType.Visible)
                RemoveVisiblePlaybackObject(entry);
        }
    }
    
    private void OnPlaybackObjectInserted(object? sender, IndexedCollectionEntry<PlaybackObject> e)
    {
        if (e.Item.Type == PlaybackObjectType.Visible)
            InsertVisiblePlaybackObject(e);
        
        e.Item.PropertyChanged += OnPlaybackObjectPropertyChanged;
    }

    private void OnPlaybackObjectRemoved(object? sender, IndexedCollectionEntry<PlaybackObject> e)
    {
        if (e.Item.Type == PlaybackObjectType.Visible)
            RemoveVisiblePlaybackObject(e);
        
        e.Item.PropertyChanged -= OnPlaybackObjectPropertyChanged;
    }

    private void InsertVisiblePlaybackObject(IndexedCollectionEntry<PlaybackObject> entry)
    {
        var playbackObject = entry.Item;
        if (playbackObject.CustomShapeInfo is not null)
        {
            objectIndexToCacheIndex.EnsureCount(entry.Index + 1);
            objectIndexToCacheIndex[entry.Index] = CreateMeshCacheHandle(playbackObject.CustomShapeInfo);
        }
    }
    
    private void RemoveVisiblePlaybackObject(IndexedCollectionEntry<PlaybackObject> entry)
    {
        var playbackObject = entry.Item;
        if (playbackObject.CustomShapeInfo is not null)
        {
            var cacheIndex = objectIndexToCacheIndex[entry.Index];
            if (cacheIndex.HasValue)
            {
                DestroyMeshCacheHandle(cacheIndex.Value);
                objectIndexToCacheIndex[entry.Index] = null;
            }
        }
    }
    
    private void OnPlaybackObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not PlaybackObject playbackObject)
            return;
        
        var index = playbackObjects.GetIndexForId(playbackObject.Id);

        switch (e.PropertyName)
        {
            case nameof(PlaybackObject.Type):
                if (playbackObject.Type == PlaybackObjectType.Visible)
                    InsertVisiblePlaybackObject(new IndexedCollectionEntry<PlaybackObject>(playbackObject, index));
                else
                    RemoveVisiblePlaybackObject(new IndexedCollectionEntry<PlaybackObject>(playbackObject, index));
                break;
            case nameof(PlaybackObject.CustomShapeInfo):
                if (playbackObject.Type == PlaybackObjectType.Visible)
                {
                    if (index < objectIndexToCacheIndex.Count)
                    {
                        var oldMeshCacheHandle = objectIndexToCacheIndex[index];
                        if (oldMeshCacheHandle.HasValue)
                            DestroyMeshCacheHandle(oldMeshCacheHandle.Value);
                        objectIndexToCacheIndex[index] = null;
                    }

                    if (playbackObject.CustomShapeInfo is not null)
                    {
                        objectIndexToCacheIndex.EnsureCount(index + 1);
                        objectIndexToCacheIndex[index] = CreateMeshCacheHandle(playbackObject.CustomShapeInfo);
                    }
                }
                break;
        }
    }
    
    public bool TryGetMesh(int objectIndex, out MeshHandle meshHandle)
    {
        if (objectIndex >= 0 && objectIndex < objectIndexToCacheIndex.Count)
        {
            var meshCacheHandle = objectIndexToCacheIndex[objectIndex];
            if (meshCacheHandle.HasValue && cacheItems.TryGetItem(meshCacheHandle.Value, out var cacheItem))
            {
                meshHandle = cacheItem.MeshHandle;
                return true;
            }
        }
        meshHandle = default;
        return false;
    }

    private int CreateMeshCacheHandle(VGShapeInfo shapeInfo)
    {
        var id = GetShapeInfoId(shapeInfo);
        var index = cacheItems.GetIndexForId(id);
        if (cacheItems.TryGetItem(index, out var cacheItem))
        {
            cacheItem.RefCount++;
            return index;
        }

        var mesh = VGShape.GenerateMesh(shapeInfo);
        var meshHandle = renderQueue.CreateMesh(mesh.Vertices, mesh.Indices);
        cacheItem = new CacheItem(id, meshHandle)
        {
            RefCount = 1
        };
        cacheItems.Insert(cacheItem);
        return index;
    }
    
    private void DestroyMeshCacheHandle(int index)
    {
        if (cacheItems.TryGetItem(index, out var cacheItem))
        {
            cacheItem.RefCount--;
            if (cacheItem.RefCount <= 0)
            {
                renderQueue.DestroyMesh(cacheItem.MeshHandle);
                cacheItems.Remove(index);
            }
        }
    }

    private static Identifier GetShapeInfoId(VGShapeInfo shapeInfo)
    {
        var sidesHash = NumberUtil.ComputeHash(shapeInfo.Sides);
        var roundnessHash = NumberUtil.ComputeHash(shapeInfo.Roundness);
        var thicknessHash = NumberUtil.ComputeHash(shapeInfo.Thickness);
        var sliceCountHash = NumberUtil.ComputeHash(shapeInfo.SliceCount);
        return NumberUtil.Mix(sidesHash, roundnessHash, thicknessHash, sliceCountHash);
    }
}