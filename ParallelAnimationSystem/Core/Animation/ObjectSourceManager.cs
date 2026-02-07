using ParallelAnimationSystem.Core.Model;

namespace ParallelAnimationSystem.Core.Animation;

public class ObjectSourceManager(PlaybackObjectContainer playbackObjects) : IDisposable
{
    private readonly MainObjectSource mainObjectSource = new(playbackObjects);

    private BeatmapData? attachedBeatmapData;

    public void AttachBeatmapData(BeatmapData beatmapData)
    {
        if (attachedBeatmapData is not null)
            throw new InvalidOperationException($"A {nameof(BeatmapData)} is already attached");
        
        // add existing objects
        foreach (var beatmapObject in beatmapData.Objects.Values)
            mainObjectSource.InsertBeatmapObject(beatmapObject);
        
        // attach events
        beatmapData.ObjectInserted += OnBeatmapObjectInserted;
        beatmapData.ObjectRemoved += OnBeatmapObjectRemoved;
    }

    public void DetachBeatmapData()
    {
        if (attachedBeatmapData is null)
            throw new InvalidOperationException($"No {nameof(BeatmapData)} is attached");
        
        // remove existing objects
        foreach (var beatmapObject in attachedBeatmapData.Objects.Values)
            mainObjectSource.RemoveBeatmapObject(beatmapObject);
        
        // detach events
        attachedBeatmapData.ObjectInserted -= OnBeatmapObjectInserted;
        attachedBeatmapData.ObjectRemoved -= OnBeatmapObjectRemoved;
        
        attachedBeatmapData = null;
    }

    private void OnBeatmapObjectInserted(object? sender, BeatmapObject e)
    {
        mainObjectSource.InsertBeatmapObject(e);
    }
    
    private void OnBeatmapObjectRemoved(object? sender, BeatmapObject e)
    {
        mainObjectSource.RemoveBeatmapObject(e);
    }

    public void Dispose()
    {
        if (attachedBeatmapData is not null)
        {
            // detach events
            attachedBeatmapData.ObjectInserted -= OnBeatmapObjectInserted;
            attachedBeatmapData.ObjectRemoved -= OnBeatmapObjectRemoved;
        }
        
        mainObjectSource.Dispose();
    }
}