using System.Numerics;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Model;

public class BeatmapEventList
{
    public event EventHandler<KeyframeList<EventKeyframe<Vector2>>>? CameraPositionKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<float>>>? CameraScaleKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<float>>>? CameraRotationKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<string>>>? ThemeKeyframesChanged;
    
    public KeyframeList<EventKeyframe<Vector2>> CameraPosition { get; } = [];
    public KeyframeList<EventKeyframe<float>> CameraScale { get; } = [];
    public KeyframeList<EventKeyframe<float>> CameraRotation { get; } = [];
    public KeyframeList<EventKeyframe<string>> Theme { get; } = [];

    public BeatmapEventList()
    {
        CameraPosition.ListChanged += (_, e) => CameraPositionKeyframesChanged?.Invoke(this, e);
        CameraScale.ListChanged += (_, e) => CameraScaleKeyframesChanged?.Invoke(this, e);
        CameraRotation.ListChanged += (_, e) => CameraRotationKeyframesChanged?.Invoke(this, e);
        Theme.ListChanged += (_, e) => ThemeKeyframesChanged?.Invoke(this, e);
    }
}