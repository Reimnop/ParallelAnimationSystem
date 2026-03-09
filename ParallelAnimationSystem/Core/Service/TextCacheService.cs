using System.ComponentModel;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Rendering;
using ParallelAnimationSystem.Rendering.Handle;
using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Core.Service;

public class TextCacheService : IDisposable
{
    private readonly IRenderQueue renderQueue;
    private readonly PlaybackObjectContainer playbackObjects;

    private readonly List<TextHandle?> textHandles = [];

    public TextCacheService(IRenderQueue renderQueue, PlaybackObjectContainer playbackObjects)
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
        if (playbackObject.Text is not null)
        {
            textHandles.EnsureCount(entry.Index + 1);
            textHandles[entry.Index] = renderQueue.CreateText(playbackObject.Text);
        }
    }
    
    private void RemoveVisiblePlaybackObject(IndexedCollectionEntry<PlaybackObject> entry)
    {
        var playbackObject = entry.Item;
        if (playbackObject.Text is not null)
        {
            var textHandle = textHandles[entry.Index];
            if (textHandle.HasValue)
            {
                renderQueue.DestroyText(textHandle.Value);
                textHandles[entry.Index] = null;
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
            case nameof(PlaybackObject.Text):
                if (playbackObject.Type == PlaybackObjectType.Visible)
                {
                    if (index < textHandles.Count)
                    {
                        var oldTextHandle = textHandles[index];
                        if (oldTextHandle.HasValue)
                            renderQueue.DestroyText(oldTextHandle.Value);
                        textHandles[index] = null;
                    }

                    if (playbackObject.Text is not null)
                    {
                        textHandles.EnsureCount(index + 1);
                        textHandles[index] = renderQueue.CreateText(playbackObject.Text);
                    }
                }
                break;
        }
    }
    
    public bool TryGetText(int objectIndex, out TextHandle textHandle)
    {
        if (objectIndex >= 0 && objectIndex < textHandles.Count)
        {
            var handle = textHandles[objectIndex];
            if (handle.HasValue)
            {
                textHandle = handle.Value;
                return true;
            }
        }
        textHandle = default;
        return false;
    }
}