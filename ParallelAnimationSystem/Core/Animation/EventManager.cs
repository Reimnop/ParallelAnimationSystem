using System.Numerics;
using ParallelAnimationSystem.Core.Data;
using ParallelAnimationSystem.Core.Model;
using ParallelAnimationSystem.Mathematics;

namespace ParallelAnimationSystem.Core.Animation;

public class EventManager
{
    private readonly Sequence<Vector2> cameraPositionSequence = new(Vector2.Lerp, () => Vector2.Zero);
    private readonly Sequence<float> cameraRotationSequence = new(float.Lerp, () => 0f);
    private readonly Sequence<float> cameraScaleSequence = new(float.Lerp, () => 20f);

    private readonly EventState state = new();
    
    private BeatmapData? attachedBeatmapData;
    
    public void AttachBeatmapData(BeatmapData beatmapData)
    {
        if (attachedBeatmapData is not null)
            throw new InvalidOperationException($"A {nameof(BeatmapData)} is already attached");
        
        attachedBeatmapData = beatmapData;
        
        // load events
        cameraPositionSequence.LoadKeyframes(ResolveCameraPositionKeyframes(beatmapData.Events.CameraPosition));
        cameraRotationSequence.LoadKeyframes(ResolveCameraRotationKeyframes(beatmapData.Events.CameraRotation));
        cameraScaleSequence.LoadKeyframes(ResolveCameraScaleKeyframes(beatmapData.Events.CameraScale));
        
        // attach events
        beatmapData.Events.CameraPositionKeyframesChanged += OnEventsCameraPositionKeyframesChanged;
        beatmapData.Events.CameraRotationKeyframesChanged += OnEventsCameraRotationKeyframesChanged;
        beatmapData.Events.CameraScaleKeyframesChanged += OnEventsCameraScaleKeyframesChanged;
    }
    
    public void DetachBeatmapData()
    {
        if (attachedBeatmapData is null)
            return;
        
        // unload events
        cameraPositionSequence.LoadKeyframes([]);
        cameraRotationSequence.LoadKeyframes([]);
        cameraScaleSequence.LoadKeyframes([]);
        
        // detach events
        attachedBeatmapData.Events.CameraPositionKeyframesChanged -= OnEventsCameraPositionKeyframesChanged;
        attachedBeatmapData.Events.CameraRotationKeyframesChanged -= OnEventsCameraRotationKeyframesChanged;
        attachedBeatmapData.Events.CameraScaleKeyframesChanged -= OnEventsCameraScaleKeyframesChanged;
        
        attachedBeatmapData = null;
    }

    public EventState ComputeEventAt(float time, ThemeColorState tcs)
    {
        state.CameraPosition = cameraPositionSequence.ComputeValueAt(time);
        state.CameraRotation = cameraRotationSequence.ComputeValueAt(time);
        state.CameraScale = cameraScaleSequence.ComputeValueAt(time);
        return state;
    }

    private void OnEventsCameraPositionKeyframesChanged(object? sender, KeyframeList<EventKeyframe<Vector2>> e)
    {
        cameraPositionSequence.LoadKeyframes(ResolveCameraPositionKeyframes(e));
    }

    private void OnEventsCameraRotationKeyframesChanged(object? sender, KeyframeList<EventKeyframe<float>> e)
    {
        cameraRotationSequence.LoadKeyframes(ResolveCameraRotationKeyframes(e));
    }

    private void OnEventsCameraScaleKeyframesChanged(object? sender, KeyframeList<EventKeyframe<float>> e)
    {
        cameraScaleSequence.LoadKeyframes(ResolveCameraScaleKeyframes(e));
    }

    private IEnumerable<Keyframe<Vector2>> ResolveCameraPositionKeyframes(KeyframeList<EventKeyframe<Vector2>> eventsCameraPosition)
    {
        foreach (var eventKeyframe in eventsCameraPosition)
            yield return new Keyframe<Vector2>(eventKeyframe.Time, eventKeyframe.Ease, eventKeyframe.Value);
    }

    private IEnumerable<Keyframe<float>> ResolveCameraRotationKeyframes(KeyframeList<EventKeyframe<float>> eventsCameraRotation)
    {
        foreach (var eventKeyframe in eventsCameraRotation)
            yield return new Keyframe<float>(eventKeyframe.Time, eventKeyframe.Ease, MathUtil.DegreesToRadians(eventKeyframe.Value));
    }

    private IEnumerable<Keyframe<float>> ResolveCameraScaleKeyframes(KeyframeList<EventKeyframe<float>> eventsCameraScale)
    {
        foreach (var eventKeyframe in eventsCameraScale)
            yield return new Keyframe<float>(eventKeyframe.Time, eventKeyframe.Ease, eventKeyframe.Value);
    }
}