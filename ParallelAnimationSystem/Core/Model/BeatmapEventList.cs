using System.Numerics;
using Pamx.Common.Data;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Model;

public class BeatmapEventList
{
    public event EventHandler<KeyframeList<EventKeyframe<Vector2>>>? CameraPositionKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<float>>>? CameraScaleKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<float>>>? CameraRotationKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<float>>>? CameraShakeKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<string>>>? ThemeKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<float>>>? ChromaKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<BloomData>>>? BloomKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<VignetteData>>>? VignetteKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<LensDistortionData>>>? LensDistortionKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<GrainData>>>? GrainKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<GradientData>>>? GradientKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<GlitchData>>>? GlitchKeyframesChanged;
    public event EventHandler<KeyframeList<EventKeyframe<float>>>? HueKeyframesChanged;
    
    public KeyframeList<EventKeyframe<Vector2>> CameraPosition { get; } = [];
    public KeyframeList<EventKeyframe<float>> CameraScale { get; } = [];
    public KeyframeList<EventKeyframe<float>> CameraRotation { get; } = [];
    public KeyframeList<EventKeyframe<float>> CameraShake { get; } = [];
    public KeyframeList<EventKeyframe<string>> Theme { get; } = [];
    public KeyframeList<EventKeyframe<float>> Chroma { get; } = [];
    public KeyframeList<EventKeyframe<BloomData>> Bloom { get; } = [];
    public KeyframeList<EventKeyframe<VignetteData>> Vignette { get; } = [];
    public KeyframeList<EventKeyframe<LensDistortionData>> LensDistortion { get; } = [];
    public KeyframeList<EventKeyframe<GrainData>> Grain { get; } = [];
    public KeyframeList<EventKeyframe<GradientData>> Gradient { get; } = [];
    public KeyframeList<EventKeyframe<GlitchData>> Glitch { get; } = [];
    public KeyframeList<EventKeyframe<float>> Hue { get; } = [];

    public BeatmapEventList()
    {
        CameraPosition.ListChanged += (_, e) => CameraPositionKeyframesChanged?.Invoke(this, e);
        CameraScale.ListChanged += (_, e) => CameraScaleKeyframesChanged?.Invoke(this, e);
        CameraRotation.ListChanged += (_, e) => CameraRotationKeyframesChanged?.Invoke(this, e);
        CameraShake.ListChanged += (_, e) => CameraShakeKeyframesChanged?.Invoke(this, e);
        Theme.ListChanged += (_, e) => ThemeKeyframesChanged?.Invoke(this, e);
        Chroma.ListChanged += (_, e) => ChromaKeyframesChanged?.Invoke(this, e);
        Bloom.ListChanged += (_, e) => BloomKeyframesChanged?.Invoke(this, e);
        Vignette.ListChanged += (_, e) => VignetteKeyframesChanged?.Invoke(this, e);
        LensDistortion.ListChanged += (_, e) => LensDistortionKeyframesChanged?.Invoke(this, e);
        Grain.ListChanged += (_, e) => GrainKeyframesChanged?.Invoke(this, e);
        Gradient.ListChanged += (_, e) => GradientKeyframesChanged?.Invoke(this, e);
        Glitch.ListChanged += (_, e) => GlitchKeyframesChanged?.Invoke(this, e);
        Hue.ListChanged += (_, e) => HueKeyframesChanged?.Invoke(this, e);
    }
}