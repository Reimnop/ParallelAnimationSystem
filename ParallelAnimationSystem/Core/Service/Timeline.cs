using System.ComponentModel;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Service;

public class Timeline : IDisposable
{
    private enum ObjectEventType
    {
        Spawn,
        Kill
    }

    private class ObjectEvent
    {
        public required float Time { get; init; }
        public required ObjectEventType Type { get; init; }
        public required int ObjectIndex { get; init; }
    }
    
    private class ObjectEventComparer : IComparer<ObjectEvent>
    {
        public int Compare(ObjectEvent? x, ObjectEvent? y)
        {
            if (x is null && y is null)
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;
            
            var timeComparison = x.Time.CompareTo(y.Time);
            if (timeComparison != 0)
                return timeComparison;
            
            return x.Type.CompareTo(y.Type); // spawn before kill
        }
    }

    private readonly HashSet<int> aliveObjects = [];

    private readonly List<ObjectEvent> objectEvents = [];
    private int currentEventIndex;

    private float previousTime = float.NegativeInfinity;
    
    private readonly PlaybackObjectContainer playbackObjects;

    private readonly HashSet<int> objectsPendingForEventUpdate = [];
    private readonly HashSet<int> objectsPendingForInsertion = [];
    private readonly HashSet<int> objectsPendingForRemoval = [];
    
    private readonly ObjectEventComparer objectEventComparer = new();
    
    public Timeline(PlaybackObjectContainer playbackObjects)
    {
        this.playbackObjects = playbackObjects;
        
        // insert all existing playback objects
        foreach (var entry in playbackObjects)
            AttachPlaybackObject(entry);

        // attach events
        this.playbackObjects.PlaybackObjectInserted += OnPlaybackObjectInserted;
        this.playbackObjects.PlaybackObjectRemoved += OnPlaybackObjectRemoved;
    }
    
    public void Dispose()
    {
        // detach events
        foreach (var (playbackObject, _) in playbackObjects)
            playbackObject.PropertyChanged -= OnPlaybackObjectPropertyChanged;
        
        playbackObjects.PlaybackObjectInserted -= OnPlaybackObjectInserted;
        playbackObjects.PlaybackObjectRemoved -= OnPlaybackObjectRemoved;
    }

    private void OnPlaybackObjectInserted(object? sender, IndexedCollectionEntry<PlaybackObject> entry)
        => AttachPlaybackObject(entry);

    private void OnPlaybackObjectRemoved(object? sender, IndexedCollectionEntry<PlaybackObject> entry)
        => DetachPlaybackObject(entry);

    private void AttachPlaybackObject(IndexedCollectionEntry<PlaybackObject> entry)
    {
        if (entry.Item.IsVisible)
            InsertObjectForPlayback(entry.Index);
        
        entry.Item.PropertyChanged += OnPlaybackObjectPropertyChanged;
    }
    
    private void DetachPlaybackObject(IndexedCollectionEntry<PlaybackObject> entry)
    {
        if (entry.Item.IsVisible)
            RemoveObjectFromPlayback(entry.Index);
        
        entry.Item.PropertyChanged -= OnPlaybackObjectPropertyChanged;
    }

    private void OnPlaybackObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not PlaybackObject playbackObject)
            return;

        var index = playbackObjects.GetIndexForId(playbackObject.Id);
        
        switch (e.PropertyName)
        {
            case nameof(PlaybackObject.StartTime):
            case nameof(PlaybackObject.EndTime):
                objectsPendingForEventUpdate.Add(index);
                break;
            case nameof(PlaybackObject.IsVisible):
                if (playbackObject.IsVisible)
                    InsertObjectForPlayback(index);
                else
                    RemoveObjectFromPlayback(index);
                break;
        }
    }
    
    private void InsertObjectForPlayback(int index)
    {
        if (objectsPendingForRemoval.Remove(index))
            return;
        
        objectsPendingForInsertion.Add(index);
    }
    
    private void RemoveObjectFromPlayback(int index)
    {
        if (objectsPendingForInsertion.Remove(index))
            return;
        
        objectsPendingForRemoval.Add(index);
    }

    public IReadOnlyCollection<int> ComputeAliveObjects(float time)
    {
        UpdateObjectEvents();
        UpdateAliveObjects(time);
        return aliveObjects;
    }

    private void UpdateObjectEvents()
    {
        var shouldRecalculateIndex = false;
        
        if (objectsPendingForInsertion.Count > 0)
        {
            foreach (var playbackObject in objectsPendingForInsertion)
                objectsPendingForEventUpdate.Add(playbackObject);
            
            objectsPendingForInsertion.Clear();
        }

        if (objectsPendingForRemoval.Count > 0)
        {
            // remove all events
            objectEvents.RemoveAll(x => objectsPendingForRemoval.Contains(x.ObjectIndex));

            // remove from aliveObjects
            foreach (var playbackObject in objectsPendingForRemoval)
                aliveObjects.Remove(playbackObject);

            objectsPendingForRemoval.Clear();
            
            shouldRecalculateIndex = true;
        }

        if (objectsPendingForEventUpdate.Count > 0)
        {
            // remove current events
            objectEvents.RemoveAll(x => objectsPendingForEventUpdate.Contains(x.ObjectIndex));
        
            // re-insert updated events
            objectEvents.AddRange(objectsPendingForEventUpdate
                .Select<int, IndexedCollectionEntry<PlaybackObject>?>(x => playbackObjects.TryGetItem(x, out var item) 
                    ? new IndexedCollectionEntry<PlaybackObject>(item, x)
                    : null)
                .SelectMany(x => x.HasValue
                    ? GetObjectEvents(x.Value)
                    : []));
            objectEvents.Sort(objectEventComparer);
        
            // clear pending set
            objectsPendingForEventUpdate.Clear();
            
            shouldRecalculateIndex = true;
        }

        if (shouldRecalculateIndex)
        {
            // forces recalculation of currentEventIndex
            // TODO: Make this more efficient
            previousTime = float.NegativeInfinity;
            currentEventIndex = 0;
        }
    }

    private static IEnumerable<ObjectEvent> GetObjectEvents(IndexedCollectionEntry<PlaybackObject> entry)
    {
        yield return new ObjectEvent
        {
            Time = entry.Item.StartTime,
            Type = ObjectEventType.Spawn,
            ObjectIndex = entry.Index
        };
        
        yield return new ObjectEvent
        {
            Time = entry.Item.EndTime,
            Type = ObjectEventType.Kill,
            ObjectIndex = entry.Index
        };
    }

    private void UpdateAliveObjects(float time)
    {
        if (objectEvents.Count == 0)
            return;
        
        if (time == previousTime) // don't do anything if we're paused
            return;

        if (time > previousTime) // forward time
        {
            while (currentEventIndex < objectEvents.Count && time >= objectEvents[currentEventIndex].Time)
            {
                var objectEvent = objectEvents[currentEventIndex];
                
                switch (objectEvent.Type)
                {
                    case ObjectEventType.Spawn:
                        aliveObjects.Add(objectEvent.ObjectIndex);
                        break;
                    case ObjectEventType.Kill:
                        aliveObjects.Remove(objectEvent.ObjectIndex);
                        break;
                }

                currentEventIndex++;
            }
        }
        else // reverse time
        {
            while (currentEventIndex - 1 >= 0 && time < objectEvents[currentEventIndex - 1].Time)
            {
                var objectEvent = objectEvents[currentEventIndex - 1];
                
                switch (objectEvent.Type)
                {
                    case ObjectEventType.Spawn:
                        aliveObjects.Remove(objectEvent.ObjectIndex);
                        break;
                    case ObjectEventType.Kill:
                        aliveObjects.Add(objectEvent.ObjectIndex);
                        break;
                }
                
                currentEventIndex--;
            }
        }
        
        previousTime = time;
    }
}