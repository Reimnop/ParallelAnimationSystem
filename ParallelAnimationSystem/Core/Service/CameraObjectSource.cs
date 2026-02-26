using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Core.Service;

public class CameraObjectSource : IDisposable
{
    private readonly PlaybackObjectContainer playbackObjects;
    private readonly PlaybackObject playbackObject;

    private readonly Identifier id;
    
    private BeatmapEvents? events;
    
    public CameraObjectSource(PlaybackObjectContainer playbackObjects)
    {
        this.playbackObjects = playbackObjects;
        
        // get identifier
        id = Identifier.FromString("camera");
        
        // create playback object
        playbackObject = new PlaybackObject(id)
        {
            StartTime = 0f,
            EndTime = float.PositiveInfinity,
            Type = PlaybackObjectType.Camera
        };
        
        // add to container
        playbackObjects.Insert(playbackObject);
    }

    public void Dispose()
    {
        DetachEvents();
        
        var index = playbackObjects.GetIndexForId(id);
        playbackObjects.Remove(index);
    }

    public void AttachEvents(BeatmapEvents events)
    {
        if (this.events is not null)
            throw new InvalidOperationException($"A {nameof(BeatmapEvents)} is already attached");
        
        this.events = events;
        
        playbackObject.PositionSequence.LoadKeyframes(ResolveCameraPositionKeyframes(events.CameraPosition));
        playbackObject.ScaleSequence.LoadKeyframes(ResolveCameraScaleKeyframes(events.CameraScale));
        playbackObject.RotationSequence.LoadKeyframes(ResolveCameraRotationKeyframes(events.CameraRotation));
        
        this.events.CameraPositionKeyframesChanged += OnCameraPositionKeyframesChanged;
        this.events.CameraScaleKeyframesChanged += OnCameraScaleKeyframesChanged;
        this.events.CameraRotationKeyframesChanged += OnCameraRotationKeyframesChanged;
    }

    public void DetachEvents()
    {
        if (events is null)
            return;
        
        playbackObject.PositionSequence.LoadKeyframes([]);
        playbackObject.ScaleSequence.LoadKeyframes([]);
        playbackObject.RotationSequence.LoadKeyframes([]);
        
        events.CameraPositionKeyframesChanged -= OnCameraPositionKeyframesChanged;
        events.CameraScaleKeyframesChanged -= OnCameraScaleKeyframesChanged;
        events.CameraRotationKeyframesChanged -= OnCameraRotationKeyframesChanged;
        
        events = null;
    }
    
    private void OnCameraPositionKeyframesChanged(object? sender, KeyframeList<Keyframe<Vector2>> e)
    {
        playbackObject.PositionSequence.LoadKeyframes(ResolveCameraPositionKeyframes(e));
    }
    
    private void OnCameraScaleKeyframesChanged(object? sender, KeyframeList<Keyframe<float>> e)
    {
        playbackObject.ScaleSequence.LoadKeyframes(ResolveCameraScaleKeyframes(e));
    }
    
    private void OnCameraRotationKeyframesChanged(object? sender, KeyframeList<Keyframe<float>> e)
    {
        playbackObject.RotationSequence.LoadKeyframes(ResolveCameraRotationKeyframes(e));
    }

    private static IEnumerable<BakedKeyframe<Vector2>> ResolveCameraPositionKeyframes(IEnumerable<Keyframe<Vector2>> keyframes)
    {
        foreach (var kf in keyframes)
            yield return new BakedKeyframe<Vector2>(kf.Time, kf.Ease, kf.Value);
    }
    
    private static IEnumerable<BakedKeyframe<Vector2>> ResolveCameraScaleKeyframes(IEnumerable<Keyframe<float>> keyframes)
    {
        foreach (var kf in keyframes)
            yield return new BakedKeyframe<Vector2>(kf.Time, kf.Ease, Vector2.One * (kf.Value / 20.0f));
    }
    
    private static IEnumerable<BakedKeyframe<float>> ResolveCameraRotationKeyframes(IEnumerable<Keyframe<float>> keyframes)
    {
        foreach (var kf in keyframes)
            yield return new BakedKeyframe<float>(kf.Time, kf.Ease, MathUtil.DegreesToRadians(kf.Value));
    }
}