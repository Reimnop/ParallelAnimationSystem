using System.ComponentModel;

namespace ParallelAnimationSystem.Core.Animation;

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
        public required PlaybackObject Object { get; init; }
    }

    private readonly HashSet<PlaybackObject> aliveObjects = [];

    private readonly List<ObjectEvent> objectEvents = [];
    private int currentEventIndex;

    private float previousTime = float.NegativeInfinity;
    
    private readonly PlaybackObjectContainer playbackObjects;

    private readonly HashSet<PlaybackObject> objectsPendingForEventUpdate = [];
    private readonly HashSet<PlaybackObject> objectsPendingForInsertion = [];
    private readonly HashSet<PlaybackObject> objectsPendingForRemoval = [];
    
    public Timeline(PlaybackObjectContainer playbackObjects)
    {
        this.playbackObjects = playbackObjects;
        
        // insert all existing playback objects
        foreach (var playbackObject in playbackObjects)
            OnPlaybackObjectInserted(null, playbackObject);

        // attach events
        this.playbackObjects.PlaybackObjectInserted += OnPlaybackObjectInserted;
        this.playbackObjects.PlaybackObjectRemoved += OnPlaybackObjectRemoved;
    }
    
    public void Dispose()
    {
        // detach events
        foreach (var playbackObject in playbackObjects)
            playbackObject.PropertyChanged -= OnPlaybackObjectPropertyChanged;
        
        playbackObjects.PlaybackObjectInserted -= OnPlaybackObjectInserted;
        playbackObjects.PlaybackObjectRemoved -= OnPlaybackObjectRemoved;
    }

    private void OnPlaybackObjectInserted(object? sender, PlaybackObject e)
    {
        if (e.IsVisible)
            InsertObjectForPlayback(e);
        
        e.PropertyChanged += OnPlaybackObjectPropertyChanged;
    }

    private void OnPlaybackObjectRemoved(object? sender, PlaybackObject e)
    {
        if (e.IsVisible)
            RemoveObjectFromPlayback(e);
        
        e.PropertyChanged -= OnPlaybackObjectPropertyChanged;
    }

    private void OnPlaybackObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not PlaybackObject playbackObject)
            return;
        
        switch (e.PropertyName)
        {
            case nameof(PlaybackObject.StartTime):
            case nameof(PlaybackObject.EndTime):
                objectsPendingForEventUpdate.Add(playbackObject);
                break;
            case nameof(PlaybackObject.IsVisible):
                if (playbackObject.IsVisible)
                    InsertObjectForPlayback(playbackObject);
                else
                    RemoveObjectFromPlayback(playbackObject);
                break;
        }
    }
    
    private void InsertObjectForPlayback(PlaybackObject playbackObject)
    {
        if (objectsPendingForRemoval.Remove(playbackObject))
            return;
        
        objectsPendingForInsertion.Add(playbackObject);
    }
    
    private void RemoveObjectFromPlayback(PlaybackObject playbackObject)
    {
        if (objectsPendingForInsertion.Remove(playbackObject))
            return;
        
        objectsPendingForRemoval.Add(playbackObject);
    }

    public IReadOnlyCollection<PlaybackObject> ComputeAliveObjects(float time)
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
            objectEvents.RemoveAll(x => objectsPendingForRemoval.Contains(x.Object));

            // remove from aliveObjects
            foreach (var playbackObject in objectsPendingForRemoval)
                aliveObjects.Remove(playbackObject);

            objectsPendingForRemoval.Clear();
            
            shouldRecalculateIndex = true;
        }

        if (objectsPendingForEventUpdate.Count > 0)
        {
            // remove current events
            objectEvents.RemoveAll(x => objectsPendingForEventUpdate.Contains(x.Object));
        
            // re-insert updated events
            objectEvents.AddRange(objectsPendingForEventUpdate.SelectMany(GetObjectEvents));
            objectEvents.Sort((a, b) => a.Time.CompareTo(b.Time));
        
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

    private static IEnumerable<ObjectEvent> GetObjectEvents(PlaybackObject playbackObject)
    {
        yield return new ObjectEvent
        {
            Time = playbackObject.StartTime,
            Type = ObjectEventType.Spawn,
            Object = playbackObject
        };
        
        yield return new ObjectEvent
        {
            Time = playbackObject.EndTime,
            Type = ObjectEventType.Kill,
            Object = playbackObject
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
                        aliveObjects.Add(objectEvent.Object);
                        break;
                    case ObjectEventType.Kill:
                        aliveObjects.Remove(objectEvent.Object);
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
                        aliveObjects.Remove(objectEvent.Object);
                        break;
                    case ObjectEventType.Kill:
                        aliveObjects.Add(objectEvent.Object);
                        break;
                }
                
                currentEventIndex--;
            }
        }
        
        previousTime = time;
    }
}