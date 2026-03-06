using System.ComponentModel;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Service;

public class MeshCacheService
{
    private readonly IRenderingFactory renderingFactory;
    private readonly PlaybackObjectContainer playbackObjects;

    private readonly List<MeshHandle?> meshHandles = [];

    public MeshCacheService(IRenderingFactory renderingFactory, PlaybackObjectContainer playbackObjects)
    {
        this.renderingFactory = renderingFactory;
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
        if (playbackObject.CustomShapeMesh is not null)
        {
            meshHandles.EnsureCount(entry.Index + 1);
            meshHandles[entry.Index] = renderingFactory.CreateMesh(playbackObject.CustomShapeMesh.Vertices, playbackObject.CustomShapeMesh.Indices);
        }
    }
    
    private void RemoveVisiblePlaybackObject(IndexedCollectionEntry<PlaybackObject> entry)
    {
        var playbackObject = entry.Item;
        if (playbackObject.CustomShapeMesh is not null)
        {
            var meshHandle = meshHandles[entry.Index];
            if (meshHandle.HasValue)
            {
                renderingFactory.DestroyMesh(meshHandle.Value);
                meshHandles[entry.Index] = null;
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
            case nameof(PlaybackObject.CustomShapeMesh):
                if (playbackObject.Type == PlaybackObjectType.Visible)
                {
                    if (index < meshHandles.Count)
                    {
                        var oldMeshHandle = meshHandles[index];
                        if (oldMeshHandle.HasValue)
                            renderingFactory.DestroyMesh(oldMeshHandle.Value);
                        meshHandles[index] = null;
                    }

                    if (playbackObject.CustomShapeMesh is not null)
                    {
                        meshHandles.EnsureCount(index + 1);
                        meshHandles[index] = renderingFactory.CreateMesh(playbackObject.CustomShapeMesh.Vertices, playbackObject.CustomShapeMesh.Indices);
                    }
                }
                break;
        }
    }
    
    public bool TryGetMesh(int objectIndex, out MeshHandle meshHandle)
    {
        if (objectIndex >= 0 && objectIndex < meshHandles.Count)
        {
            var handle = meshHandles[objectIndex];
            if (handle.HasValue)
            {
                meshHandle = handle.Value;
                return true;
            }
        }
        meshHandle = default;
        return false;
    }
}