using System.Numerics;
using Pamx.Common.Data;
using ParallelAnimationSystem.Core.Data;

namespace ParallelAnimationSystem.Core.Model;

public class BeatmapEvents
{
    public event EventHandler<KeyframeList<Data.Keyframe<Vector2>>>? CameraPositionKeyframesChanged;
    public event EventHandler<KeyframeList<Data.Keyframe<float>>>? CameraScaleKeyframesChanged;
    public event EventHandler<KeyframeList<Data.Keyframe<float>>>? CameraRotationKeyframesChanged;
    public event EventHandler<KeyframeList<Data.Keyframe<float>>>? CameraShakeKeyframesChanged;
    public event EventHandler<KeyframeList<Data.Keyframe<string>>>? ThemeKeyframesChanged;
    public event EventHandler<KeyframeList<Data.Keyframe<float>>>? ChromaKeyframesChanged;
    public event EventHandler<KeyframeList<Data.Keyframe<BloomData>>>? BloomKeyframesChanged;
    public event EventHandler<KeyframeList<Data.Keyframe<VignetteData>>>? VignetteKeyframesChanged;
    public event EventHandler<KeyframeList<Data.Keyframe<LensDistortionData>>>? LensDistortionKeyframesChanged;
    public event EventHandler<KeyframeList<Data.Keyframe<GrainData>>>? GrainKeyframesChanged;
    public event EventHandler<KeyframeList<Data.Keyframe<GradientData>>>? GradientKeyframesChanged;
    public event EventHandler<KeyframeList<Data.Keyframe<GlitchData>>>? GlitchKeyframesChanged;
    public event EventHandler<KeyframeList<Data.Keyframe<float>>>? HueKeyframesChanged;
    
    public KeyframeList<Data.Keyframe<Vector2>> CameraPosition { get; } = [];
    public KeyframeList<Data.Keyframe<float>> CameraScale { get; } = [];
    public KeyframeList<Data.Keyframe<float>> CameraRotation { get; } = [];
    public KeyframeList<Data.Keyframe<float>> CameraShake { get; } = [];
    public KeyframeList<Data.Keyframe<string>> Theme { get; } = [];
    public KeyframeList<Data.Keyframe<float>> Chroma { get; } = [];
    public KeyframeList<Data.Keyframe<BloomData>> Bloom { get; } = [];
    public KeyframeList<Data.Keyframe<VignetteData>> Vignette { get; } = [];
    public KeyframeList<Data.Keyframe<LensDistortionData>> LensDistortion { get; } = [];
    public KeyframeList<Data.Keyframe<GrainData>> Grain { get; } = [];
    public KeyframeList<Data.Keyframe<GradientData>> Gradient { get; } = [];
    public KeyframeList<Data.Keyframe<GlitchData>> Glitch { get; } = [];
    public KeyframeList<Data.Keyframe<float>> Hue { get; } = [];

    public BeatmapEvents()
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